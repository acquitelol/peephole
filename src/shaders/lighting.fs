#version 330

in vec2 fragTexCoord;
in vec3 fragNormal;
in vec3 fragPos;

out vec4 finalColor;

uniform sampler2D texture0;
uniform sampler2D texture2;

uniform vec3 viewPos;

uniform bool shadows;
uniform bool fog;
uniform bool lights;

#define MATERIAL_DIFFUSE 0
#define MATERIAL_REFLECTIVE 1
uniform int material;

float fogStart = 2;
float fogEnd = 10;

float shadowStart = 4;
float shadowEnd = 8;

float lightRange = 10;
float lightFactor = 0.5;

void main() {
    float lightDistance = length(viewPos - fragPos);
    float attenuation = (1.0 - smoothstep(0.0, 1.0, lightDistance / lightRange));
    // float attenuation = clamp(1.0 - lightDistance / lightRange, 0.0, 1.0);
    // float attenuation = 1 / (1 + 0.05 * lightDistance + 0.001 * lightDistance * lightDistance);
    // if (material == MATERIAL_REFLECTIVE) attenuation *= attenuation;
    attenuation *= attenuation;

    vec3 albedo = texture(texture0, fragTexCoord).rgb;
    vec3 ambient = 0.1 * albedo;

    vec3 normalTex = texture(texture2, fragTexCoord).xyz * 2 - 1;

    vec3 N = normalize(fragNormal);
    vec3 dp1 = dFdx(fragPos);
    vec3 dp2 = dFdy(fragPos);
    vec2 duv1 = dFdx(fragTexCoord);
    vec2 duv2 = dFdy(fragTexCoord);

    vec3 T = normalize(dp1 * duv2.y - dp2 * duv1.y);
    vec3 B = normalize(cross(N, T));
    mat3 TBN = mat3(T, B, N);

    vec3 normal = normalize(TBN * normalTex);
    vec3 lightDir = normalize(viewPos - fragPos);
    float diffuseAmount = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = vec3(lightFactor * diffuseAmount * albedo * attenuation);

    // vec3 viewDir = normalize(viewPos - fragPos);
    // vec3 reflectDir = reflect(-lightDir, normal);
    // float specularFactor = 32;
    // // if (material == MATERIAL_DIFFUSE) specularFactor = 32;
    // // else if (material == MATERIAL_REFLECTIVE) specularFactor = 8;
    // // else specularFactor = 32;

    // float specularAmount = pow(max(dot(viewDir, reflectDir), 0.0), specularFactor);
    // vec3 specular = vec3(lightFactor * specularAmount * attenuation);
    vec3 viewDir = normalize(viewPos - fragPos);
    vec3 halfDir = normalize(lightDir + viewDir);

    float specularAmount = pow(max(dot(normal, halfDir), 0.0), 128);
    vec3 specular = vec3(lightFactor * specularAmount * attenuation);
    vec3 color = lights ? ambient + diffuse + specular : albedo;

    float viewDistance = length(fragPos - viewPos);
    float fogFactor = smoothstep(fogStart, fogEnd, viewDistance);
    float shadowFactor = smoothstep(shadowStart, shadowEnd, length(fragPos));

    vec3 atmosphereColor = vec3(0);

    vec3 shadowedColor = mix(color, atmosphereColor, shadows ? shadowFactor : 0.0);
    vec3 foggedColor = mix(shadowedColor, atmosphereColor, fog ? fogFactor : 0.0);

    finalColor = vec4(foggedColor, 1.0);
}
