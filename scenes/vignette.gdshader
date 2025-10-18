shader_type canvas_item;


void fragment() {
	float dist = distance(vec2(UV.x, UV.y), vec2(0.5, 0.5));
	if (dist > 0.3) {
		COLOR.a = (dist - 0.3);
	}
	else {
		COLOR.a = 0.0;
	}
}
