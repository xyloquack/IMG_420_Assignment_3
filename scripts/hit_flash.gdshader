shader_type canvas_item;

uniform bool enable = false;
uniform float opacity = 0.0;

void fragment() {
	if (enable && COLOR.a == 1.0)
	{
		COLOR = vec4(COLOR.r + opacity * (1.0 - COLOR.r), COLOR.g + opacity * (1.0 - COLOR.g), COLOR.b + opacity * (1.0 - COLOR.b), 1.0);
	}
}