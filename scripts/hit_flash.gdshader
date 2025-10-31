shader_type canvas_item;

uniform float opacity = 0.0;
uniform vec4 flash_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
	if (COLOR.a == 1.0)
	{
		COLOR = vec4(mix(COLOR.rgb, flash_color.rgb, opacity), 1.0);
	}
}