#include "xy_gl.h"

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

Matrix orthoProjMatrix(float left, float right, float bottom, float top, float near, float far)
{
	Matrix m = Matrix::identity();
	m[0][0] = 2.0f / (right - left);
	m[0][3] = -(right + left) / (right - left);
	m[1][1] = 2.0f / (top - bottom);
	m[1][3] = -(top + bottom) / (top - bottom);
	m[2][2] = -2.0f / (far - near);
	m[2][3] = -(far + near) / (far - near);

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

void clearZBuffer(TGAImage *zBuffer)
{
	for (int i = 0; i < zBuffer->get_width(); ++i)
	{
		for (int j = 0; j < zBuffer->get_height(); ++j)
		{
			zBuffer->set(i, j, TGAColor(255));
		}
	}
}

void drawTriangleFillOptimizeZBuffer(Vec4f *points, IShader *shader, TGAImage *colorBuffer, TGAImage *zBuffer)
{
	Vec2f boxMin;
	Vec2f boxMax;
	Vec2f clampMax(zBuffer->get_width() - 1, zBuffer->get_height() - 1);
	findBoundingBox<Vec4f, Vec2f, float>(points, boxMin, boxMax, clampMax);

	Vec4f p;
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
			for (int i = 0; i < 3; ++i)
			{
				p[2] += points[i][2] * bc[i];
			}

			int x = int(p[0]);
			int y = int(p[1]);
			int depth = round(p[2] * 255);
			if (depth < zBuffer->get(x, y)[0])
			{
				TGAColor color;
				if (shader->fragment(bc, color))
				{
					zBuffer->set(x, y, TGAColor(depth));
					if (colorBuffer != nullptr)
					{
						colorBuffer->set(x, y, color);
					}
				}
			}
		}
	}
}

Vec3f reflect(const Vec3f& v, const Vec3f& n)
{
	return n * (n * v) * 2 - v;
}

void Pipeline::setViewport(float x, float y, float width, float height, float minDepth, float maxDepth)
{
	m_viewportMtx = viewportMatrix(x, y, width, height, minDepth, maxDepth);
}

void Pipeline::render(Model *model)
{
	for (int i = 0; i < model->nfaces(); ++i)
	{
		Vec4f clipCoords[3];
		Vec4f ndc[3];
		Vec4f screenCoords[3];

		for (int j = 0; j < 3; ++j)
		{
			clipCoords[j] = m_shader->vertex(i, j);
			ndc[j] = clipCoords[j] / clipCoords[j][3];
			screenCoords[j] = m_viewportMtx * ndc[j];
			screenCoords[j][0] = round(screenCoords[j][0]);
			screenCoords[j][1] = round(screenCoords[j][1]);
		}

		Vec3f faceNormal = cross(proj<3>(screenCoords[1] - screenCoords[0]), proj<3>(screenCoords[2] - screenCoords[0]));
		if (faceNormal.z > 0)
		{
			drawTriangleFillOptimizeZBuffer(screenCoords, m_shader, m_colorBuffer, m_zBuffer);
		}
	}
}
