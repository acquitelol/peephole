#version 330

in vec2 fragTexCoord;
in vec3 fragNormal;
in vec3 fragPos;
in mat3 TBN;

out vec4 finalColor;

uniform sampler2D texture0;
uniform sampler2D texture2;
uniform sampler2D mraMap;

uniform vec3 viewPos;

uniform bool shadows;
uniform bool fog;
uniform bool lightsEnabled;
uniform bool useTexMRA;

#define MATERIAL_DIFFUSE 0
#define MATERIAL_REFLECTIVE 1
uniform int material;

#define LIGHT_DIRECTIONAL 0
#define LIGHT_POINT 1
#define LIGHT_SPOT 2

#define MAX_LIGHTS 8
#define PI 3.141592

struct Light {
    int type;
    vec3 position;
    vec3 target;
    vec3 color;
    float strength;
};

uniform int lightCount;
uniform Light lights[MAX_LIGHTS];

float fogStart = 2;
float fogEnd = 10;

float shadowStart = 8;
float shadowEnd = 16;

float lightRange = 50;
float lightFactor = 0.5;

float metallicValue = 0.5;
float roughnessValue = 0.5;
float aoValue = 0.5;

vec3 SchlickFresnel(float hDotV, vec3 refl)
{
    return refl + (1.0 - refl) * pow(1.0 - hDotV, 5.0);
}

float GgxDistribution(float nDotH, float roughness)
{
    float a = roughness * roughness * roughness * roughness;
    float d = nDotH * nDotH * (a - 1.0) + 1.0;
    d = PI * d * d;
    return a / max(d, 0.0000001);
}

float GeomSmith(float nDotV, float nDotL, float roughness)
{
    float r = roughness + 1.0;
    float k = r * r / 8.0;
    float ik = 1.0 - k;
    float ggx1 = nDotV / (nDotV * ik + k);
    float ggx2 = nDotL / (nDotL * ik + k);
    return ggx1 * ggx2;
}

void main() {
    vec3 albedo = texture(texture0, fragTexCoord).rgb;

    float metallic = clamp(metallicValue, 0.0, 1.0);
    float roughness = clamp(roughnessValue, 0.0, 1.0);
    float ao = clamp(aoValue, 0.0, 1.0);

    if (useTexMRA) {
        vec4 mra = texture(mraMap, fragTexCoord);
        metallic = clamp(mra.r + metallicValue, 0.04, 1.0);
        roughness = clamp(mra.g + roughnessValue, 0.04, 1.0);
        ao = (mra.b + aoValue) * 0.5;
    }

    vec3 normalTex = texture(texture2, fragTexCoord).xyz * 2 - 1;

    // vec3 N = normalize(fragNormal);
    // vec3 dp1 = dFdx(fragPos);
    // vec3 dp2 = dFdy(fragPos);
    // vec2 duv1 = dFdx(fragTexCoord);
    // vec2 duv2 = dFdy(fragTexCoord);

    // vec3 T = normalize(dp1 * duv2.y - dp2 * duv1.y);
    // vec3 B = normalize(dp2 * duv1.x - dp1 * duv2.x);
    // mat3 TBN = mat3(T, B, N);

    vec3 normal = normalize(TBN * normalTex);
    // finalColor = vec4(normal, 1);
    // return;

    vec3 V = normalize(viewPos - fragPos);
    vec3 baseRefl = mix(vec3(0.04), albedo, metallic);
    vec3 lightAccum = vec3(0.0);

    for (int i = 0; i < lightCount; i++)
    {
        float dist = length(lights[i].position - fragPos);

        vec3 L;
        float attenuation;

        if (lights[i].type == LIGHT_DIRECTIONAL) {
            L = normalize(lights[i].position - lights[i].target);
            attenuation = 1.0;
        } else {
            L = normalize(lights[i].position - fragPos);
            attenuation = 1.0 - smoothstep(0.0, 1.0, dist / lightRange);
        }

        vec3 H = normalize(V + L);
        vec3 radiance = lights[i].color * lights[i].strength * attenuation;
        // finalColor = lights[i].color;
        float nDotV = max(dot(normal, V), 0.0000001);
        float nDotL = max(dot(normal, L), 0.0000001);
        float hDotV = max(dot(H, V), 0.0);
        float nDotH = max(dot(normal, H), 0.0);

        float D = GgxDistribution(nDotH, roughness);
        float G = GeomSmith(nDotV, nDotL, roughness);
        vec3 F = SchlickFresnel(hDotV, baseRefl);

        vec3 spec = (D * G * F) / (4.0 * nDotV * nDotL);

        vec3 kD = vec3(1.0) - F;
        kD *= 1.0 - metallic; // metals have no diffuse

        lightAccum += (kD * albedo / PI + spec) * radiance * nDotL;
    }

    vec3 ambientFinal = albedo * ao * 0.5;
    vec3 pbrColor = lightAccum + ambientFinal;
    vec3 color = lightsEnabled ? pbrColor : albedo;

    float viewDistance = length(fragPos - viewPos);
    float fogFactor = smoothstep(fogStart, fogEnd, viewDistance);
    float shadowFactor = smoothstep(shadowStart, shadowEnd, length(fragPos));

    vec3 atmosphereColor = vec3(0);

    vec3 shadowedColor = mix(color, atmosphereColor, shadows ? shadowFactor : 0.0);
    vec3 foggedColor = mix(shadowedColor, atmosphereColor, fog ? fogFactor : 0.0);

    finalColor = vec4(foggedColor, 1.0);
}
