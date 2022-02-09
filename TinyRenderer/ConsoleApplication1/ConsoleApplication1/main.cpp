#include <algorithm>

#include "ThirdParty/tgaimage.h"
#include "ThirdParty/geometry.h"
#include "ThirdParty/model.h"

using namespace std;

const TGAColor white(255, 255, 255, 255);
const TGAColor red(255, 0,   0,   255);
const TGAColor green(0, 255, 0, 255);
const int width = 1920;
const int height = 1080;
const float pi = 3.1415926;
Model *model = nullptr;

void drawLine(int xBegin, int yBegin, int xEnd, int yEnd, TGAImage &image, TGAColor color)
{
	bool swapXAndY = false;
	if (abs(xBegin - xEnd) < abs(yBegin - yEnd))
	{
		swap(xBegin, yBegin);
		swap(xEnd, yEnd);
		swapXAndY = true;
	}

	if (xBegin > xEnd)
	{
		swap(xBegin, xEnd);
		swap(yBegin, yEnd);
	}

	int dX = xEnd - xBegin;
	int dY = yEnd - yBegin;
	float dError = float(dY) / dX;
	float error = yBegin;
	int y = yBegin;

	for (int x = xBegin; x <= xEnd; ++x)
	{
		if (swapXAndY)
		{
			image.set(y, x, color);
		}
		else
		{
			image.set(x, y, color);
		}

		error += dError;
		y = round(error);
	}
}

void drawLine(const Vec2i &p0, const Vec2i &p1, TGAImage &image, TGAColor color)
{
	drawLine(p0.x, p0.y, p1.x, p1.y, image, color);
}

void drawTriangleWireFrame(Vec2i *points, TGAImage &image, TGAColor color)
{
	drawLine(points[0], points[1], image, color);
	drawLine(points[1], points[2], image, color);
	drawLine(points[2], points[0], image, color);
}

void drawTriangleFill(Vec2i *points, TGAImage &image, TGAColor color)
{
	if (points[0].y > points[1].y)
		swap(points[0], points[1]);
	if (points[0].y > points[2].y)
		swap(points[0], points[2]);
	if (points[1].y > points[2].y)
		swap(points[1], points[2]);

	int totalHeight = points[2].y - points[0].y;
	int segmentHeight = points[1].y - points[0].y;
	if (segmentHeight == 0)
	{
		segmentHeight = 1;
	}

	for (int y = points[0].y; y <= points[1].y; ++y)
	{
		float alpha = float(y - points[0].y) / totalHeight;
		float beta = float(y - points[0].y) / segmentHeight;
		Vec2i A = points[0] + (points[2] - points[0])*alpha;
		Vec2i B = points[0] + (points[1] - points[0])*beta;

		if (A.x > B.x)
		{
			swap(A, B);
		}
		for (int x = A.x; x <= B.x; ++x)
		{
			image.set(x, y, color);
		}
	}

	segmentHeight = points[2].y - points[1].y;
	if (segmentHeight == 0)
	{
		segmentHeight = 1;
	}

	for (int y = points[1].y; y <= points[2].y; ++y)
	{
		float alpha = float(y - points[0].y) / totalHeight;
		float beta = float(y - points[1].y) / segmentHeight;
		Vec2i A = points[0] + (points[2] - points[0])*alpha;
		Vec2i B = points[1] + (points[2] - points[1])*beta;

		if (A.x > B.x)
		{
			swap(A, B);
		}
		for (int x = A.x; x <= B.x; ++x)
		{
			image.set(x, y, color);
		}
	}
}

Vec3f cross(const Vec3f &v0, const Vec3f &v1)
{
	float x = v0.y * v1.z - v0.z * v1.y;
	float y = v0.z * v1.x - v0.x * v1.z;
	float z = v0.x * v1.y - v0.y * v1.x;

	return Vec3f(x, y, z);
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

void drawTriangleFillOptimize(Vec2i *points, TGAImage &image, TGAColor color)
{
	Vec2i boxMin;
	Vec2i boxMax;
	Vec2i clampMax(image.get_width() - 1, image.get_height() - 1);
	findBoundingBox<Vec2i, Vec2i, int>(points, boxMin, boxMax, clampMax);

	Vec2i p;
	for (p.x = boxMin.x; p.x <= boxMax.x; ++p.x)
	{
		for (p.y = boxMin.y; p.y <= boxMax.y; ++p.y)
		{
			Vec3f bc = barycentric<Vec2i>(points, p);
			if (bc.x < 0 || bc.y < 0 || bc.z < 0)
			{
				continue;
			}

			image.set(p.x, p.y, color);
		}
	}
}

Matrix lookAtMatrix(const Vec3f &eye, const Vec3f &center, const Vec3f &up)
{
	Matrix m = Matrix::identity();
	Vec3f z = (eye - center).normalize();
	Vec3f x = cross(up, z).normalize();
	Vec3f y = cross(z, x).normalize();
	for (int i = 0; i < 3; ++i)
	{
		m[0][i] = x[i];
		m[1][i] = y[i];
		m[2][i] = z[i];
	}
	m[0][3] = -(x * eye);
	m[1][3] = -(y * eye);
	m[2][3] = -(z * eye);

	return m;
}

Matrix perspectiveProjMatrix(float fovY, float aspectRatio, float near, float far)
{
	Matrix m = Matrix::identity();
	m[0][0] = 1 / (aspectRatio * tan(fovY / 2));
	m[1][1] = 1 / tan(fovY / 2);
	m[2][2] = -(far + near) / (far - near);
	m[2][3] = -2 * far * near / (far - near);
	m[3][2] = -1.0f;
	m[3][3] = 0.0f;

	return m;
}

Matrix viewportMatrix(float x, float y, float width, float height, float minDepth, float maxDepth)
{
	Matrix m = Matrix::identity();
	m[0][0] = width / 2;
	m[0][3] = x + width / 2;
	m[1][1] = height / 2;
	m[1][3] = y + height / 2;
	m[2][2] = (maxDepth - minDepth) / 2;
	m[2][3] = (maxDepth + minDepth) / 2;

	return m;
}

void drawTriangleFillOptimizeZBuffer(Vec4f *points, Vec2f *uvs, float **zBuffer, TGAImage &image, float intensity)
{
	Vec2f boxMin;
	Vec2f boxMax;
	Vec2f clampMax(image.get_width() - 1, image.get_height() - 1);
	findBoundingBox<Vec4f, Vec2f, float>(points, boxMin, boxMax, clampMax);

	Vec4f p;
	Vec2f uv;
	for (p[0] = boxMin.x; p[0] <= boxMax.x; ++p[0])
	{
		for (p[1] = boxMin.y; p[1] <= boxMax.y; ++p[1])
		{
			Vec3f bc = barycentric<Vec4f>(points, p);
			if (bc.x < 0 || bc.y < 0 || bc.z < 0)
			{
				continue;
			}

			p[2] = 0.0f;
			uv.x = 0;
			uv.y = 0;
			for (int i = 0; i < 3; ++i)
			{
				p[2] += points[i][2] * bc[i];
				uv = uv + uvs[i] * bc[i];
			}

			if (p[2] < zBuffer[int(p[0])][int(p[1])])
			{
				zBuffer[int(p[0])][int(p[1])] = p[2];
				TGAColor diffuseColor = model->diffuse(uv);
				image.set(p[0], p[1], diffuseColor * intensity);
			}
		}
	}
}

int main(int argc, char** argv)
{
    TGAImage image(width, height, TGAImage::RGB);

	float **zBuffer = new float*[width];
	for (int i = 0; i < width; ++i)
	{
		zBuffer[i] = new float[height];
	}
	for (int i = 0; i < width; ++i)
	{
		for (int j = 0; j < height; ++j)
		{
			zBuffer[i][j] = numeric_limits<float>::max();
		}
	}

	//drawLine(13, 20, 80, 40, image, white);

	//Vec2i t0[3] = { Vec2i(10, 70), Vec2i(50, 160), Vec2i(70, 80) };
	//Vec2i t1[3] = { Vec2i(180, 50), Vec2i(150, 1), Vec2i(70, 180) };
	//Vec2i t2[3] = { Vec2i(180, 150), Vec2i(120, 160), Vec2i(130, 180) };

	//drawTriangleWireFrame(t0, image, red);
	//drawTriangleWireFrame(t1, image, white);
	//drawTriangleWireFrame(t2, image, green);

	//drawTriangleFill(t0, image, red);
	//drawTriangleFill(t1, image, white);
	//drawTriangleFill(t2, image, green);

	//drawTriangleFillOptimize(t0, image, red);
	//drawTriangleFillOptimize(t1, image, white);
	//drawTriangleFillOptimize(t2, image, green);

	model = new Model("obj/african_head.obj");
	Vec3f lightDir(0.0f, 0.0f, -1.0f);
	Matrix modelMtx = Matrix::identity();
	Matrix viewMtx = lookAtMatrix(Vec3f(1.0f, 1.0f, 3.0f), Vec3f(0.0f, 0.0f, 0.0f), Vec3f(0.0f, 1.0f, 0.0f));
	Matrix perspectiveProjMtx = perspectiveProjMatrix(pi*0.25f, float(width)/height, 1.0f, 1000.0f);
	Matrix viewportMtx = viewportMatrix(0.0f, 0.0f, float(width), float(height), 0.0f, 1.0f);

	for (int i = 0; i < model->nfaces(); ++i)
	{
		vector<int> face = model->face(i);
		Vec4f modelCoords[3];
		Vec4f worldCoords[3];
		Vec4f viewCoords[3];
		Vec4f clipCoords[3];
		Vec4f ndc[3];
		Vec4f screenCoords[3];
		Vec2f uvCoords[3];
		for (int j = 0; j < 3; ++j)
		{
			modelCoords[j] = embed<4>(model->vert(face[j]));
			worldCoords[j] = modelMtx * modelCoords[j];
			viewCoords[j] = viewMtx * worldCoords[j];
			clipCoords[j] = perspectiveProjMtx * viewCoords[j];
			ndc[j] = clipCoords[j] / clipCoords[j][3];
			screenCoords[j] = viewportMtx * ndc[j];
			screenCoords[j][0] = round(screenCoords[j][0]);
			screenCoords[j][1] = round(screenCoords[j][1]);
			uvCoords[j] = model->uv(i, j);
		}

		Vec3f n = cross(proj<3>(viewCoords[1] - viewCoords[0]), proj<3>(viewCoords[2] - viewCoords[0]));
		n.normalize();
		float intensity = -(lightDir * n);
		if (intensity > 0)
		{
			drawTriangleFillOptimizeZBuffer(screenCoords, uvCoords, zBuffer, image, intensity);
		}
	}

    image.flip_vertically(); // i want to have the origin at the left bottom corner of the image
    image.write_tga_file("output.tga");

	delete model;
	model = nullptr;

	for (int i = 0; i < width; ++i)
	{
		delete[] zBuffer[i];
	}
	delete[] zBuffer;
	zBuffer = nullptr;

    return 0;
}

