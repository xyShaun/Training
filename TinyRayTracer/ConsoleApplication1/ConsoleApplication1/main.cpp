#include <fstream>
#include <algorithm>
#include "ThirdParty/geometry.h"

struct Material
{
	Material() {}

	Material(const Vec4f &weights, const Vec3f &diffuseColor, float specularExponent, float refractiveIndex)
		: m_weights(weights),
		m_diffuseColor(diffuseColor),
		m_specularColor(Vec3f(1.0f, 1.0f, 1.0f)),
		m_specularExponent(specularExponent),
		m_refractiveIndex(refractiveIndex)
	{

	}

	Vec4f m_weights;
	Vec3f m_diffuseColor;
	Vec3f m_specularColor;
	float m_specularExponent;
	float m_refractiveIndex;
};

struct Light
{
	Light(const Vec3f &position, const float &intensity)
		: m_position(position),
		m_intensity(intensity)
	{

	}

	Vec3f m_position;
	float m_intensity;
};

struct Sphere
{
	Sphere(const Vec3f &center, float radius, const Material &material)
		: m_center(center),
		m_radius(radius),
		m_material(material)
	{

	}

	bool intersectsWithRay(const Vec3f &origin, const Vec3f &direction, float &dist) const
	{
		Vec3f m = origin - m_center;
		Vec3f dir = direction;
		dir.normalize();

		float a = dir * dir;
		float b = m * dir * 2;
		float c = m * m - m_radius * m_radius;

		float delta = b * b - 4 * a*c;
		if (delta < 0.0f)
		{
			return false;
		}

		float sqrtDelta = sqrtf(delta);
		float x1 = (-b - sqrtDelta) / 2 * a;
		float x2 = (-b + sqrtDelta) / 2 * a;
		if (x1 < 0 && x2 < 0)
		{
			return false;
		}
		else if (x1 < 0)
		{
			dist = x2;
		}
		else
		{
			dist = x1;
		}

		return true;
	}

	Vec3f m_center;
	float m_radius;
	Material m_material;
};

Vec3f reflect(const Vec3f& ri, const Vec3f& n)
{
	return n * (n * ri) * 2 - ri;
}

Vec3f refract(const Vec3f &i, const Vec3f &n, float refractiveIndex)
{
	float cosTheta1 = i * n * -1.0f;
	float eta1 = 1.0f;
	float eta2 = refractiveIndex;
	Vec3f normal = n;

	if (cosTheta1 < 0.0f)
	{
		cosTheta1 = -cosTheta1;
		std::swap(eta1, eta2);
		normal = n * -1.0f;
	}

	float etaRatio = eta1 / eta2;
	float k = 1 - etaRatio * etaRatio * (1 - cosTheta1 * cosTheta1);
	if (k < 0.0f)
	{
		return Vec3f(0.0f, 0.0f, 0.0f);
	}
	else
	{
		return i * etaRatio + normal * (etaRatio * cosTheta1 - sqrtf(k));
	}
}

bool sceneIntersectsWithRay(const Vec3f &origin, const Vec3f &direction, const std::vector<Sphere> &spheres,
	Vec3f &hit, Vec3f &normal, Material &material)
{
	float spheresDist = std::numeric_limits<float>::max();
	for (int i = 0; i < spheres.size(); ++i)
	{
		float dist;
		if (spheres[i].intersectsWithRay(origin, direction, dist) && dist < spheresDist)
		{
			spheresDist = dist;
			hit = origin + direction * dist;
			normal = (hit - spheres[i].m_center).normalize();
			material = spheres[i].m_material;
		}
	}

	// plane equation y=-4
	float checkerboardDist = std::numeric_limits<float>::max();
	if (abs(direction.y) > 1e-3)
	{
		Vec3f planeNormal = Vec3f(0.0f, 1.0f, 0.0f);
		Vec3f planePoint = Vec3f(0.0f, -4.0f, 0.0f);
		float d = -(planeNormal * planePoint);
		float dist = (-planeNormal * origin - d) / (planeNormal * direction);
		Vec3f planeHit = origin + direction * dist;

		if (dist > 0.0f && abs(planeHit.x) < 10.0f && planeHit.z < -10.0f && planeHit.z > -30.0f && dist < spheresDist)
		{
			checkerboardDist = dist;
			hit = planeHit;
			normal = planeNormal;
			material.m_weights = Vec4f(1.0f, 0.0f, 0.0f, 0.0f);
			material.m_diffuseColor = (int(0.5f * hit.x + 10.0f) + int(0.5f * hit.z)) & 1 ? Vec3f(1.0f, 1.0f, 1.0f) : Vec3f(1.0f, 0.7f, 0.3f);
			material.m_diffuseColor = material.m_diffuseColor * 0.3f;
			material.m_specularColor = Vec3f(0.0f, 0.0f, 0.0f);
			material.m_specularExponent = 0.0f;
			material.m_refractiveIndex = 1.0f;
		}
	}

	return std::min(spheresDist, checkerboardDist) < 1000.0f;
}

Vec3f castRay(const Vec3f &origin, const Vec3f &direction, const std::vector<Sphere> &spheres, const std::vector<Light> &lights, int depth = 0)
{
	Vec3f hit;
	Vec3f normal;
	Material material;
	if (depth > 4 || !sceneIntersectsWithRay(origin, direction, spheres, hit, normal, material))
	{
		return Vec3f(0.2f, 0.7f, 0.8f);
	}

	Vec3f reflectedDir = reflect(direction * -1.0f, normal).normalize();
	Vec3f refractedDir = refract(direction, normal, material.m_refractiveIndex).normalize();
	Vec3f reflectedOrigin = reflectedDir * normal < 0.0f ? hit - normal * 1e-3 : hit + normal * 1e-3;
	Vec3f refractedOrigin = refractedDir * normal < 0.0f ? hit - normal * 1e-3 : hit + normal * 1e-3;
	Vec3f reflectedColor = castRay(reflectedOrigin, reflectedDir, spheres, lights, depth + 1);
	Vec3f refractedColor = castRay(refractedOrigin, refractedDir, spheres, lights, depth + 1);

	Vec3f finalColor(0.0f, 0.0f, 0.0f);
	for (int i = 0; i < lights.size(); ++i)
	{
		Vec3f reverseLightDir = (lights[i].m_position - hit).normalize();
		Vec3f reflectedLightDir = reflect(reverseLightDir, normal);

		float lightDist = (lights[i].m_position - hit).norm();
		Vec3f hitOffset = reverseLightDir * normal < 0.0f ? hit - normal * 1e-3 : hit + normal * 1e-3;
		Vec3f shadowHit;
		Vec3f shadowNormal;
		Material shadowMaterial;
		if (sceneIntersectsWithRay(hitOffset, reverseLightDir, spheres, shadowHit, shadowNormal, shadowMaterial))
		{
			float shadowHitDist = (shadowHit - hitOffset).norm();
			if (shadowHitDist < lightDist)
			{
				continue;
			}
		}

		Vec3f diffuseColor = material.m_diffuseColor * lights[i].m_intensity * std::max(0.0f, normal * reverseLightDir);
		Vec3f specularColor = material.m_specularColor * lights[i].m_intensity * pow(std::max(0.0f, direction * -1.0f * reflectedLightDir), material.m_specularExponent);
		finalColor = finalColor + diffuseColor * material.m_weights[0] + specularColor * material.m_weights[1];
	}

	finalColor = finalColor + reflectedColor * material.m_weights[2] + refractedColor * material.m_weights[3];

	float maxComponent = std::max(finalColor[0], std::max(finalColor[1], finalColor[2]));
	if (maxComponent > 1.0f)
	{
		finalColor = finalColor * (1.0f / maxComponent);
	}

	return finalColor;
}

void render(const std::vector<Sphere> &spheres, const std::vector<Light> &lights)
{
	const int width = 1024;
	const int height = 768;
	const float pi = 3.1415926;
	const float fovY = pi * 0.33f;

	std::vector<std::vector<Vec3f>> colorBuffer(width, std::vector<Vec3f>(height));
	for (int j = 0; j < height; ++j)
	{
		for (int i = 0; i < width; ++i)
		{
			float x = (2 * (i + 0.5f) / width - 1.0f) * tan(fovY / 2.0f) * float(width) / height;
			float y = (2 * (j + 0.5f) / height - 1.0f) * tan(fovY / 2.0f);
			Vec3f dir = Vec3f(x, y, -1.0f).normalize();

			//colorBuffer[i][j] = Vec3f(float(j) / height, float(i) / width, 0.0f);
			colorBuffer[i][j] = castRay(Vec3f(0.0f, 0.0f, 0.0f), dir, spheres, lights);
		}
	}

	std::ofstream ofs;
	ofs.open("output.ppm", std::ofstream::binary);
	ofs << "P6\n" << width << " " << height << "\n" << "255\n";
	for (int j = 0; j < height; ++j)
	{
		for (int i = 0; i < width; ++i)
		{
			for (int k = 0; k < 3; ++k)
			{
				ofs << char(255 * colorBuffer[i][height - 1 - j][k]);
			}
		}
	}

	ofs.close();
}

int main()
{
	Material ivory(Vec4f(0.6f, 0.3f, 0.1f, 0.0f), Vec3f(0.4f, 0.4f, 0.3f), 50.0f, 1.0f);
	Material glass(Vec4f(0.0f, 0.5f, 0.1f, 0.8f), Vec3f(0.6f, 0.7f, 0.8f), 125.0f, 1.5f);
	Material redRubber(Vec4f(0.9f, 0.1f, 0.0f, 0.0f), Vec3f(0.3f, 0.1f, 0.1f), 10.0f, 1.0f);
	Material mirror(Vec4f(0.0f, 10.0f, 0.8f, 0.0f), Vec3f(1.0f, 1.0f, 1.0f), 1425.0f, 1.0f);

	std::vector<Sphere> spheres;
	spheres.push_back(Sphere(Vec3f(-3.0f, 0.0f, -16.0f), 2.0f, ivory));
	spheres.push_back(Sphere(Vec3f(-1.0f, -1.5f, -12.0f), 2.0f, glass));
	spheres.push_back(Sphere(Vec3f(1.5f, -0.5f, -18.0f), 3.0f, redRubber));
	spheres.push_back(Sphere(Vec3f(7.0f, 5.0f, -18.0f), 4.0f, mirror));

	std::vector<Light> lights;
	lights.push_back(Light(Vec3f(-20.0f, 20.0f, 20.0f), 1.5f));
	lights.push_back(Light(Vec3f(30.0f, 50.0f, -25.0f), 1.8f));
	lights.push_back(Light(Vec3f(30.0f, 20.0f, 30.0f), 1.7f));

	render(spheres, lights);

	return 0;
}