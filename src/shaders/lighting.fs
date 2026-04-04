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
uniform bool useTexMRA;
uniform bool fog;

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
    vec3 dir;
    vec3 position;
    vec3 target;
    vec3 color;
    float strength;
    mat4 mat;
    sampler2D shadowMap;
};

uniform float lightIntensity;
uniform int lightCount;
uniform bool lightEnabled;
uniform Light lights[MAX_LIGHTS];

float shadowStart = 4;
float shadowEnd = 8;

float lightRange = 100;
float lightFactor = 0.5;

uniform float metallicValue;
uniform float roughnessValue;
uniform float aoValue;

vec3 SchlickFresnel(float hDotV, vec3 refl) {
    return refl + (1.0 - refl) * pow(1.0 - hDotV, 5.0);
}

float GgxDistribution(float nDotH, float roughness) {
    float a = roughness * roughness * roughness * roughness;
    float d = nDotH * nDotH * (a - 1.0) + 1.0;
    d = PI * d * d;
    return a / max(d, 0.0000001);
}

float GeomSmith(float nDotV, float nDotL, float roughness) {
    float r = roughness + 1.0;
    float k = r * r / 8.0;
    float ik = 1.0 - k;
    float ggx1 = nDotV / (nDotV * ik + k);
    float ggx2 = nDotL / (nDotL * ik + k);
    return ggx1 * ggx2;
}

float GeometricRoughness(vec3 normal) {
    vec3 dNdx = dFdx(normal);
    vec3 dNdy = dFdy(normal);
    float variance = max(dot(dNdx, dNdx), dot(dNdy, dNdy));
    return clamp(sqrt(variance) * 2.0, 0.0, 1.0);
}

void main() {
    vec3 albedo = pow(texture(texture0, fragTexCoord).rgb, vec3(2.2));

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

    float geoRoughness = GeometricRoughness(normal);
    roughness = clamp(sqrt(roughness * roughness + geoRoughness * geoRoughness), 0.04, 1.0);

    vec3 V = normalize(viewPos - fragPos);
    vec3 baseRefl = mix(vec3(0.04), albedo, metallic);
    vec3 lightAccum = vec3(0.0);

    for (int i = 0; i < lightCount; i++)
    {
        float dist = length(lights[i].position - fragPos);

        float attenuation = 1.0;
        vec3 L;

        if (lights[i].type == LIGHT_DIRECTIONAL) {
            L = -lights[i].dir;
        } else {
            L = normalize(lights[i].position - fragPos);
            // attenuation = 1 / (dist * dist);
            attenuation = 1.0 / (1.0 + dist * dist * 0.1);
            // attenuation *= pow(clamp(1.0 - dist, 0.0, 1.0), 2.0);
        }

        vec4 fragPosLightSpace = lights[i].mat * vec4(fragPos, 1.0);
        fragPosLightSpace.xyz /= fragPosLightSpace.w;
        fragPosLightSpace.xyz = fragPosLightSpace.xyz * 0.5 + 0.5;
        // finalColor = vec4(fragPosLightSpace.xy, 0, 1);
        // return;
        float closestDepth = texture(lights[i].shadowMap, fragPosLightSpace.xy).r;
        float currentDepth = fragPosLightSpace.z;
        // finalColor = texture(shadowMap, fragPosLightSpace.xy);
        // return;

        float shadow = 0.0;
        float bias = max(0.005 * (1.0 - dot(normal, L)), 0.0005);
        // if (currentDepth - bias > closestDepth) shadow = 1.0;

        vec2 texelSize = 1.0 / textureSize(lights[i].shadowMap, 0);

        for (int x = -1; x <= 1; ++x) {
            for (int y = -1; y <= 1; ++y) {
                float pcfDepth = texture(lights[i].shadowMap, fragPosLightSpace.xy + vec2(x, y) * texelSize).r;
                shadow += currentDepth - bias > pcfDepth ? 1.0 : 0.0;
            }
        }

        shadow /= 9.0;
        shadow = smoothstep(0.0, 1.0, shadow);
        // finalColor = vec4(shadow, 0, 0, 1);
        // return;

        vec3 H = normalize(V + L);
        vec3 radiance = lights[i].color * lights[i].strength * attenuation * lightIntensity;
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

        lightAccum += (kD * albedo / PI + spec) * radiance * nDotL * (1.0 - shadow);
    }

    // vec3 hemiLight = mix(vec3(0.2, 0.2, 0.25), vec3(0.8, 0.8, 0.85), normal.y);
    vec3 ambientFinal = albedo * ao * 0.5;
    vec3 color = lightEnabled ? lightAccum + ambientFinal * lightIntensity : albedo;

    float viewDistance = length(fragPos - viewPos);
    float fogFactor = 1.0 - exp(-viewDistance * viewDistance * 0.2);

    vec3 atmosphereColor = vec3(0);
    vec3 foggedColor = mix(color, atmosphereColor, fog ? fogFactor : 0.0);

    finalColor = vec4(foggedColor, 1.0);
    finalColor = vec4(pow(finalColor.rgb, vec3(1.0 / 2.2)), finalColor.a);
}
