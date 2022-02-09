#version 330 core

in vec3 vColor;
in vec2 vTexCoord;

out vec4 fragColor;

uniform sampler2D texture1;
uniform sampler2D texture2;

void main()
{
    fragColor = mix(texture(texture1, vTexCoord), texture(texture2, vTexCoord), 0.2f);
}