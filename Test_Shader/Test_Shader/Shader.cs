using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.OpenGL4;
using vec3 = OpenTK.Vector3;
using vec4 = OpenTK.Vector4;

namespace Test_Shader
{
    class Shader
    {
        public vec3 origin, direction;
        public vec4 outputColor;

        public class Material
        {
            public vec3 ambient;
            public vec3 diffuse;
            public vec3 reflection;
            public vec3 specular;
            public vec3 transparency;
            public vec3 emission;
            public vec3 atenuation;
            public float refractionCoef;
            public float shiness;
        };

        public class Sphere
        {
            public vec3 position;
            public float radius;
            //vec3 color;
            public Material material;
            public int objectid;
        };

        public class Ray
        {
            public vec3 origin;
            public vec3 direction;
            public int type;

            public Ray()
            {
                origin = direction = new vec3(0.0f);
                type = 0;
            }

            public Ray(vec3 _origin, vec3 _direction, int _type)
            {
                origin = _origin;
                direction = _direction;
                type = _type;
            }
        };

        public class RayNode
        {
            public Ray ray;
            //vec3 color;
            public vec3 reflectionColor;
            public vec3 refractionColor;
            public vec3 diffuseColor;
            public vec3 specular;
            public vec3 reflection;
            public vec3 refraction;
            public int parentIndex;
            public int depth;
        };

        public class HitInfo
        {
            public bool hitDetected;
            public vec3 hitPoint;
            public vec3 surfaceNormal;
            public float distance;
            //public vec3 color;
            public Material material;
            public int objectid;            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            public int objectType;
        };

        public const int Max_Depth = 5;
        public const int Max_Nodes = 64;
        private RayNode[] rayNode = new RayNode[Max_Nodes];
        public const int sphereCount = 3;
        public Sphere[] spheres = new Sphere[sphereCount];
        public const int TYPE_DIFFUSE = 1;
        public const int TYPE_SHADOW = 2;
        public const int TYPE_REFLECTION = 3;
        public const int TYPE_TRANSPARENCY = 4;
        public const int TYPE_SPHERE = 0;
        public const int TYPE_TRIANGLE = 1;

        public float dot(vec3 u, vec3 v)
        {
            return u.X * v.X + u.Y * v.Y + u.Z * v.Z;
        }

        public vec3 normalize(vec3 v)
        {
            v.Normalize();
            return v;
        }

        public float length(vec3 v)
        {
            return v.Length;
        }

        public float max(float a, float b)
        {
            if (a >= b)
                return a;
            else
                return b;
        }

        public void sphereIntersect(Ray ray, Sphere sphere, HitInfo hitInfo)
        {
            vec3 trackToSphere = ray.origin - sphere.position;
            float a = dot(ray.direction, ray.direction);
            float b = 2 * dot(trackToSphere, ray.direction);
            float c = dot(trackToSphere, trackToSphere) - sphere.radius * sphere.radius;
            float discriminant = b * b - 4.0f * a * c;

            if (discriminant > 0)
            {
                float distance = (-b - (float)Math.Sqrt(discriminant)) / (2.0f * a);
                if (distance > 0.0001 && (distance < hitInfo.distance && hitInfo.hitDetected || !hitInfo.hitDetected))
                {
                    hitInfo.distance = distance;
                    hitInfo.hitPoint = ray.origin + ray.direction * hitInfo.distance;
                    hitInfo.surfaceNormal = normalize(hitInfo.hitPoint - sphere.position);
                    hitInfo.hitDetected = true;
                    //hitInfo.color = sphere.color;
                    hitInfo.material = sphere.material;
                    hitInfo.objectid = sphere.objectid;
                    hitInfo.objectType = TYPE_SPHERE;
                }
            }
        }

        public bool isShadowed(vec3 hitPoint, int lightIndex, int lightType, vec3 transparency)
        {
            HitInfo hitInfoLight = new HitInfo();
            hitInfoLight.hitDetected = false;
            Ray ray = new Ray();

            if (lightType == TYPE_SPHERE)
            {
                Sphere light = spheres[lightIndex]; //?????????????????????????
                vec3 eps = normalize(light.position - hitPoint) * 0.01f;
                ray = new Ray(hitPoint + eps, normalize(light.position - hitPoint), TYPE_SHADOW);
                sphereIntersect(ray, spheres[lightIndex], hitInfoLight); //???????????????????
            }
            if (lightType == TYPE_TRIANGLE)
            {
                //triangles
            }

            float distance = hitInfoLight.distance;

            HitInfo hitInfo = new HitInfo();
            transparency = new vec3(1.0f);
            for (int i = 0; i < sphereCount; i++)
            {
                hitInfo.hitDetected = false;
                Material material = new Material();
                int type = 0, index = 0;

                if (i < sphereCount)
                {
                    index = i;
                    material = spheres[index].material;
                    type = TYPE_SPHERE;
                    sphereIntersect(ray, spheres[index], hitInfo);
                }
                else
                {
                    //triangle
                }

                if ((lightIndex != index || lightType != type) && hitInfo.hitDetected && hitInfo.distance < distance)
                {
                    if (length(material.transparency) > 0)
                    {
                        transparency *= material.transparency;
                        continue;
                    }

                    transparency = new vec3(0.0f);
                    return true;
                }
            }
            return false;
        }

        public vec3 phongShading(Material material, Material lightMaterial, vec3 hitPoint, vec3 surfaceNormal, vec3 lightDir, vec3 reflectDir, vec3 eyeDir, float distance)
        {
            float attenuation = 1.0f / (1.0f + lightMaterial.atenuation.X + distance * lightMaterial.atenuation.Y + distance * distance * lightMaterial.atenuation.Z);

            float diffuseCoef = max(0.0f, dot(surfaceNormal, lightDir));
            float specularCoef = (float)Math.Pow(dot(eyeDir, reflectDir), material.shiness);

            return material.ambient * lightMaterial.ambient + (material.diffuse * lightMaterial.emission * diffuseCoef + material.specular * lightMaterial.emission * specularCoef) * attenuation;
        }

        vec3 calculateColor(HitInfo hitInfo)
        {
            Material material = new Material();
            if (hitInfo.objectType == TYPE_SPHERE)
                material = spheres[hitInfo.objectid].material;

            vec3 hitPoint = hitInfo.hitPoint;
            vec3 surfaceNormal = hitInfo.surfaceNormal;

            if (length(material.emission) > 0.0)
                return material.emission + material.diffuse;
            vec3 resultColor = new vec3(0f);

            for (int i = 0; i < sphereCount; i++)
            {
                Material lightMaterial = new Material();
                vec3 lightPosition = new vec3();
                int lightType = 0;
                int lightIndex = 0;
                if (i < sphereCount) // ????????????????????
                {
                    lightIndex = i;
                    lightMaterial = spheres[lightIndex].material;
                    lightPosition = spheres[lightIndex].position;
                    lightType = TYPE_SPHERE; //?????????????????????????????
                }
                else
                {
                    //triangles
                }

                vec3 transparency = new vec3();

                if ((hitInfo.objectid != lightIndex || hitInfo.objectType != lightType) && length(lightMaterial.emission) > 0.0)
                {
                    vec3 currentColor = new vec3(0);

                    if (!isShadowed(hitPoint, lightIndex, lightType, transparency))
                    {
                        vec3 lightDir = lightPosition - hitPoint;
                        float distance = length(lightDir);
                        lightDir = normalize(lightDir);

                        vec3 eyeDir = normalize(origin - hitPoint);
                        vec3 reflectDir = new vec3(0);
                        if (dot(surfaceNormal, lightDir) > 0.0)
                            reflectDir = normalize(reflect(surfaceNormal, -lightDir));

                        currentColor += phongShading(material, lightMaterial, hitPoint, surfaceNormal, lightDir, reflectDir, eyeDir, distance) * transparency;
                    }

                    else
                    {
                        currentColor += lightMaterial.ambient * material.ambient;
                    }
                    resultColor += currentColor;
                }
            }

            return resultColor;
        }

        public vec3 iterativeRayTrace(Ray ray)
        {
            Material gold = new Material();
            gold.ambient = new vec3(0.25f, 0.2f, 0.07f);
            gold.diffuse = new vec3(0.75f, 0.6f, 0.23f);
            gold.reflection = new vec3(0.63f, 0.56f, 0.37f);
            gold.specular = new vec3(1.0f, 1.0f, 1.0f);
            gold.transparency = new vec3(0.0f, 0.0f, 0.0f);
            gold.emission = new vec3(0.0f, 0.0f, 0.0f);
            gold.atenuation = new vec3(1.0f, 1.0f, 6.0f);
            gold.refractionCoef = 0.0f;
            gold.shiness = 150f;

            Sphere sphere = new Sphere();
            sphere.position = new vec3(0.0f, 0.0f, 0.0f);
            sphere.radius = 0.5f;
            //sphere.color = vec3(0.9, 0.3, 0.0);
            sphere.material = gold;
            sphere.objectid = 0;
            spheres[0] = sphere;

            sphere.position = new vec3(-1.5f, 0.0f, 0.0f);
            sphere.radius = 0.2f;
            //sphere.color = vec3(0.0, 0.3, 0.7);
            sphere.material = gold;
            sphere.objectid = 1;
            spheres[1] = sphere;

            sphere.position = new vec3(-0.6f, 0.3f, 0.0f);
            sphere.radius = 0.3f;
            //sphere.color = vec3(0.5, 0.8, 0.4);
            sphere.material = gold;
            sphere.objectid = 2;
            spheres[2] = sphere;

            int numberOfNodes = 1, currentNodeIndex = 0;

            rayNode[currentNodeIndex].ray = ray;
            rayNode[currentNodeIndex].depth = 0;

            while (currentNodeIndex < numberOfNodes)
            {
                rayNode[currentNodeIndex].diffuseColor = new vec3(0);
                rayNode[currentNodeIndex].reflectionColor = new vec3(0);
                rayNode[currentNodeIndex].refractionColor = new vec3(0);

                HitInfo hitInfo = new HitInfo();
                hitInfo.hitDetected = false;
                //sphereIntersect(ray, spheres[0], hitInfo);
                //sphereIntersect(ray, spheres[1], hitInfo);
                //sphereIntersect(ray, spheres[2], hitInfo);
                for (int i = 0; i < sphereCount; i++)
                    sphereIntersect(ray, spheres[i], hitInfo);

                if (hitInfo.hitDetected)
                {
                    //float coeff = (dot(ray.direction, hitInfo.surfaceNormal) * dot(ray.direction, hitInfo.surfaceNormal)) / (dot(ray.direction, ray.direction) * dot(hitInfo.surfaceNormal, hitInfo.surfaceNormal));

                    //rayNode[currentNodeIndex].color = hitInfo.color * coeff;

                    Material material = new Material();
                    switch (hitInfo.objectType)
                    {
                        case TYPE_SPHERE: material = spheres[hitInfo.objectid].material; break;
                    }

                    rayNode[currentNodeIndex].specular = material.specular;
                    rayNode[currentNodeIndex].reflection = material.reflection;
                    rayNode[currentNodeIndex].refraction = material.transparency;

                    if (length(material.reflection) > 0.0 && rayNode[currentNodeIndex].depth < Max_Depth)
                    {
                        vec3 reflectionDir = normalize(reflect(rayNode[currentNodeIndex].ray.direction, hitInfo.surfaceNormal));
                        vec3 offset = reflectionDir * 0.01f;
                        rayNode[numberOfNodes].ray = new Ray(hitInfo.hitPoint + offset, reflectionDir, TYPE_REFLECTION);
                        rayNode[numberOfNodes].parentIndex = currentNodeIndex;
                        rayNode[numberOfNodes].depth = rayNode[currentNodeIndex].depth + 1;
                        numberOfNodes++;
                    }

                    if (length(material.transparency) > 0.0 && rayNode[currentNodeIndex].depth < Max_Depth)
                    {
                        vec3 refractionDir = normalize(refract(rayNode[currentNodeIndex].ray.direction, hitInfo.surfaceNormal, material.refractionCoef));
                        vec3 offset = refractionDir * 0.01f;
                        rayNode[numberOfNodes].ray = new Ray(hitInfo.hitPoint + offset, refractionDir, TYPE_TRANSPARENCY);
                        rayNode[numberOfNodes].parentIndex = currentNodeIndex;
                        rayNode[numberOfNodes].depth = rayNode[currentNodeIndex].depth + 1;
                        numberOfNodes++;
                    }

                    if (length(material.ambient) > 0.0 || length(material.diffuse) > 0.0 || length(material.specular) > 0.0 || rayNode[currentNodeIndex].depth >= Max_Depth)
                    {
                        rayNode[currentNodeIndex].diffuseColor = calculateColor(hitInfo);
                    }
                }
                else
                {
                    break;
                    //rayNode[currentNodeIndex].color = vec3(0.0, 0.0, 0.0);
                }

                for (int i = currentNodeIndex - 1; i > 0; i--)
                {
                    vec3 nodeColor = rayNode[i].diffuseColor + rayNode[i].reflectionColor * rayNode[i].reflection + rayNode[i].refractionColor * rayNode[i].refraction;
                    if (rayNode[i].ray.type == TYPE_REFLECTION)
                        rayNode[rayNode[i].parentIndex].reflectionColor = nodeColor;
                    else if (rayNode[i].ray.type == TYPE_TRANSPARENCY)
                        rayNode[rayNode[i].parentIndex].refractionColor = nodeColor;
                }

                currentNodeIndex++;
            }

            //return rayNode[0].color;
            return clamp(rayNode[0].diffuseColor + rayNode[0].reflectionColor * rayNode[0].reflection + rayNode[0].refractionColor * rayNode[0].refraction, new vec3(0), new vec3(1));
        }

        public void main()
        {
            Ray ray = new Ray(origin, direction, TYPE_DIFFUSE);
            outputColor = new vec4(iterativeRayTrace(ray), 1.0f);
        }

    }
}
