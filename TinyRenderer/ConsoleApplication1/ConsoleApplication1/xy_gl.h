#pragma once

#include <algorithm>

#include "ThirdParty/geometry.h"
#include "ThirdParty/tgaimage.h"
#include "ThirdParty/model.h"

using namespace std;

class IShader
{
public:
	virtual ~IShader() {}

	virtual Vec4f vertex(int face, int faceVert) = 0;
	virtual bool fragment(Vec3f barycentric, TGAColor &color) = 0;
};

class Pipeline
{
public:
	void setColorBuffer(TGAImage *colorBuffer) { m_colorBuffer = colorBuffer; }
	void setZBuffer(TGAImage *zBuffer) { m_zBuffer = zBuffer; }
	void setShader(IShader *shader) { m_shader = shader; }
	void setViewport(float x, float y, float width, float height, float minDepth, float maxDepth);
	void render(Model *model);

private:
	IShader *m_shader;
	Matrix m_viewportMtx;
	TGAImage *m_colorBuffer;
	TGAImage *m_zBuffer;
};

Matrix lookAtMatrix(const Vec3f &eye, const Vec3f &center, const Vec3f &up);
Matrix perspectiveProjMatrix(float fovY, float aspectRatio, float near, float far);
Matrix orthoProjMatrix(float left, float right, float bottom, float top, float near, float far);
Matrix viewportMatrix(float x, float y, float width, float height, float minDepth, float maxDepth);

template<typename T1, typename T2, typename T3>
void findBoundingBox(T1 *points, T2 &boxMin, T2 &boxMax, T2 &clampMax)
{
	boxMin.x = numeric_limits<T3>::max();
	boxMin.y = numeric_limits<T3>::max();
	boxMax.x = 0;
	boxMax.y = 0;

	for (int i = 0; i < 3; ++i)
	{
		for (int j = 0; j < 2; ++j)
		{
			boxMin[j] = max<T3>(0, min<T3>(boxMin[j], points[i][j]));
			boxMax[j] = min<T3>(clampMax[j], max<T3>(boxMax[j], points[i][j]));
		}
	}
}

template<typename T>
Vec3f barycentric(T *points, T p)
{
	Vec3f uv1 = cross(Vec3f(points[1][0] - points[0][0], points[2][0] - points[0][0], points[0][0] - p[0]),
		Vec3f(points[1][1] - points[0][1], points[2][1] - points[0][1], points[0][1] - p[1]));

	if (abs(uv1.z) < 1e-2)
	{
		return Vec3f(-1.0f, 1.0f, 1.0f);
	}

	return Vec3f(1.0f - (uv1.x + uv1.y) / uv1.z, uv1.x / uv1.z, uv1.y / uv1.z);
}

void clearZBuffer(TGAImage *zBuffer);
void drawTriangleFillOptimizeZBuffer(Vec4f *points, IShader *shader, TGAImage *colorBuffer, TGAImage *zBuffer);

Vec3f reflect(const Vec3f& v, const Vec3f& n);