#version 450
layout(set = 0, binding = 0) uniform texture2D _Texture;
layout(set = 0, binding = 1) uniform sampler _Sampler;
layout(location = 0) in vec2 fsin_TexCoord;
layout(location = 1) in vec4 fsin_Color;
layout(location = 0) out vec4 Output;
void main()
{
    Output = texture(sampler2D(_Texture, _Sampler), fsin_TexCoord) * fsin_Color;
}