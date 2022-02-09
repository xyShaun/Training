#include "xy_gl.h"

Model *model = nullptr;
const int width = 1920;
const int height = 1080;
const int shadowMapWidth = 2048;
const int shadowMapHeight = 2048;
const float pi = 3.1415926;

const TGAColor white(255, 255, 255, 255);
const TGAColor red(255, 0, 0, 255);
const TGAColor green(0, 255, 0, 255);

Vec3f eye(1.0f, 1.0f, 3.0f);
Vec3f center(0.0f, 0.0f, 0.0f);
Vec3f up(0.0f, 1.0f, 0.0f);
Vec3f reverseLightDir(1.0f, 1.0f, 1.0f);

Matrix modelMtx;
Matrix viewMtx;
Matrix projMtx;
Matrix lightViewMtx;
Matrix lightProjMtx;
Matrix lightViewportMtx;

class GouraudShader : public IShader
{
public:
	virtual Vec4f vertex(int face, int faceVert) override
	{
		Vec4f posL = embed<4>(model->vert(face, faceVert));
		Vec3f normalL = model->normal(face, faceVert);

		Vec4f posH = projMtx * viewMtx * modelMtx * posL;
		Vec3f normalW = modelMtx.get_minor(3, 3) * normalL;
		normalW.normalize();
		m_intensityOut[faceVert] = max(0.0f, normalW * reverseLightDir);

		return posH;
	}

	virtual bool fragment(Vec3f barycentric, TGAColor &color) override
	{
		float intensity = m_intensityOut * barycentric;
		color = white * intensity;

		return true;
	}

protected:
	Vec3f m_intensityOut;
};

class CartoonShader : public GouraudShader
{
public:
	virtual bool fragment(Vec3f barycentric, TGAColor &color) override
	{
		float intensity = m_intensityOut * barycentric;
		if (intensity > 0.85f) intensity = 1.0f;
		else if (intensity > 0.60f) intensity = 0.80f;
		else if (intensity > 0.45f) intensity = 0.60f;
		else if (intensity > 0.30f) intensity = 0.45f;
		else if (intensity > 0.15f) intensity = 0.30f;
		else intensity = 0.0f;

		color = TGAColor(255, 155, 0)*intensity;

		return true;
	}
};

class TextureShader : public GouraudShader
{
public:
	virtual Vec4f vertex(int face, int faceVert) override
	{
		Vec2f uv = model->uv(face, faceVert);
		m_uvOut.set_col(faceVert, uv);

		return GouraudShader::vertex(face, faceVert);
	}

	virtual bool fragment(Vec3f barycentric, TGAColor &color) override
	{
		float intensity = m_intensityOut * barycentric;
		Vec2f uv = m_uvOut * barycentric;
		color = model->diffuse(uv) * intensity;

		return true;
	}

protected:
	mat<2, 3, float> m_uvOut;
};

class NormalTexShader : public IShader
{
public:
	virtual Vec4f vertex(int face, int faceVert) override
	{
		Vec4f posL = embed<4>(model->vert(face, faceVert));
		Vec4f posH = projMtx * viewMtx * modelMtx * posL;

		Vec2f uv = model->uv(face, faceVert);
		m_uvOut.set_col(faceVert, uv);

		return posH;
	}

	virtual bool fragment(Vec3f barycentric, TGAColor &color) override
	{
		Vec2f uv = m_uvOut * barycentric;
		Vec3f normalL = model->normal(uv);
		Vec3f normalW = modelMtx.get_minor(3, 3) * normalL;
		normalW.normalize();
		float intensity = max(0.0f, normalW * reverseLightDir);
		color = model->diffuse(uv) * intensity;

		return true;
	}

protected:
	mat<2, 3, float> m_uvOut;
};

class SpecularTexShader : public IShader
{
public:
	virtual Vec4f vertex(int face, int faceVert) override
	{
		Vec4f posL = embed<4>(model->vert(face, faceVert));
		Vec4f posW = modelMtx * posL;
		Vec4f posH = projMtx * viewMtx * posW;
		m_posWOut.set_col(faceVert, posW);

		Vec2f uv = model->uv(face, faceVert);
		m_uvOut.set_col(faceVert, uv);

		return posH;
	}

	virtual bool fragment(Vec3f barycentric, TGAColor& color) override
	{
		Vec4f posW = m_posWOut * barycentric;
		Vec2f uv = m_uvOut * barycentric;

		Vec3f viewDir = (eye - proj<3>(posW)).normalize();
		Vec3f normalL = model->normal(uv);
		Vec3f normalW = (modelMtx.get_minor(3, 3) * normalL).normalize();
		Vec3f reflectDir = reflect(reverseLightDir, normalW);
		float alpha = model->specular(uv);
		if (abs(alpha) < 1e-6)
		{
			alpha = 1.0f;
		}

		TGAColor ambientColor(5, 5, 5);
		float intensity = max(0.0f, normalW * reverseLightDir);
		TGAColor diffuseColor = model->diffuse(uv) * intensity;
		float specular = 255 * pow(max(0.0f, viewDir * reflectDir), alpha);
		TGAColor specularColor(specular, specular, specular);

		for (int i = 0; i < 3; ++i)
		{
			color[i] = min(ambientColor[i] + diffuseColor[i] + 0.33f * specularColor[i], 255.0f);
		}

		return true;
	}

protected:
	mat<4, 3, float> m_posWOut;
	mat<2, 3, float> m_uvOut;
};

class DepthShader : public IShader
{
public:
	virtual Vec4f vertex(int face, int faceVert) override
	{
		Vec4f posL = embed<4>(model->vert(face, faceVert));
		Vec4f posH = lightProjMtx * lightViewMtx * modelMtx * posL;

		return posH;
	}

	virtual bool fragment(Vec3f barycentric, TGAColor& color) override
	{
		return true;
	}
};

class ShadowShader : public IShader
{
public:
	void setShadowMap(TGAImage *shadowMap)
	{
		m_shadowMap = shadowMap;
	}

	virtual Vec4f vertex(int face, int faceVert) override
	{
		Vec4f posL = embed<4>(model->vert(face, faceVert));
		Vec4f posW = modelMtx * posL;
		Vec4f posH = projMtx * viewMtx * posW;
		m_posWOut.set_col(faceVert, posW);

		Vec2f uv = model->uv(face, faceVert);
		m_uvOut.set_col(faceVert, uv);

		Vec4f shadowPos = lightViewportMtx * lightProjMtx * lightViewMtx * posW;
		m_shadowPosOut.set_col(faceVert, shadowPos);

		return posH;
	}

	virtual bool fragment(Vec3f barycentric, TGAColor& color) override
	{
		Vec4f posW = m_posWOut * barycentric;
		Vec2f uv = m_uvOut * barycentric;
		Vec4f shadowPos = m_shadowPosOut * barycentric;

		Vec3f viewDir = (eye - proj<3>(posW)).normalize();
		Vec3f normalL = model->normal(uv);
		Vec3f normalW = (modelMtx.get_minor(3, 3) * normalL).normalize();
		Vec3f reflectDir = reflect(reverseLightDir, normalW);
		float alpha = model->specular(uv);
		if (abs(alpha) < 1e-6)
		{
			alpha = 1.0f;
		}

		TGAColor ambientColor(5, 5, 5);
		float intensity = max(0.0f, normalW * reverseLightDir);
		TGAColor diffuseColor = model->diffuse(uv) * intensity;
		float specular = 255 * pow(max(0.0f, viewDir * reflectDir), alpha);
		TGAColor specularColor(specular, specular, specular);

		shadowPos = shadowPos / shadowPos[3];
		float shadowFactor = 1.0f;
		int x = round(shadowPos[0]);
		int y = round(shadowPos[1]);
		int curLightDepth = round(shadowPos[2] * 255);
		if (curLightDepth > m_shadowMap->get(x, y)[0] + 3)
		{
			shadowFactor = 0.3f;
		}

		for (int i = 0; i < 3; ++i)
		{
			color[i] = min(ambientColor[i] + (diffuseColor[i] + 0.33f * specularColor[i]) * shadowFactor, 255.0f);
		}

		return true;
	}

protected:
	mat<4, 3, float> m_posWOut;
	mat<2, 3, float> m_uvOut;
	mat<4, 3, float> m_shadowPosOut;
	TGAImage *m_shadowMap;
};

int main()
{
	TGAImage colorBuffer(width, height, TGAImage::RGB);
	TGAImage zBuffer(width, height, TGAImage::GRAYSCALE);
	TGAImage shadowMap(shadowMapWidth, shadowMapHeight, TGAImage::GRAYSCALE);
	clearZBuffer(&zBuffer);
	clearZBuffer(&shadowMap);

	model = new Model("obj/african_head.obj");
	//model = new Model("obj/diablo3_pose.obj");

	Pipeline pipeline;

	// Shadow Pass
	{
		modelMtx = Matrix::identity();
		lightViewMtx = lookAtMatrix(reverseLightDir, center, up);
		float halfLength = 2.0f;
		float halfDepth = 4.0f;
		lightProjMtx = orthoProjMatrix(-halfLength, halfLength, -halfLength, halfLength, 0.0f, halfDepth);

		DepthShader depthShader;
		pipeline.setColorBuffer(nullptr);
		pipeline.setZBuffer(&shadowMap);
		pipeline.setShader(&depthShader);
		pipeline.setViewport(0.0f, 0.0f, float(shadowMapWidth), float(shadowMapHeight), 0.0f, 1.0f);
		pipeline.render(model);
	}

	// Main Pass
	{
		reverseLightDir.normalize();
		modelMtx = Matrix::identity();
		viewMtx = lookAtMatrix(eye, center, up);
		projMtx = perspectiveProjMatrix(pi*0.25f, float(width) / height, 1.0f, 1000.0f);
		lightViewportMtx = viewportMatrix(0.0f, 0.0f, float(shadowMapWidth), float(shadowMapHeight), 0.0f, 1.0f);

		GouraudShader gouraudShader;
		CartoonShader cartoonShader;
		TextureShader textureShader;
		NormalTexShader normalTexShader;
		SpecularTexShader specularTexShader;
		ShadowShader shadowShader;
		shadowShader.setShadowMap(&shadowMap);
		pipeline.setColorBuffer(&colorBuffer);
		pipeline.setZBuffer(&zBuffer);
		pipeline.setShader(&shadowShader);
		pipeline.setViewport(0.0f, 0.0f, float(width), float(height), 0.0f, 1.0f);
		pipeline.render(model);
	}

	shadowMap.flip_vertically();
	shadowMap.write_tga_file("shadowMap.tga");
	colorBuffer.flip_vertically();
	colorBuffer.write_tga_file("output.tga");
	zBuffer.flip_vertically();
	zBuffer.write_tga_file("zBuffer.tga");

	delete model;
	model = nullptr;

	return 0;
}