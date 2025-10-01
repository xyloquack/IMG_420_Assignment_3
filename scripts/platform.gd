extends CollisionShape2D

func _ready():
	var polygon2D = Polygon2D.new()
	polygon2D.position = -position
	polygon2D.polygon = PackedVector2Array([Vector2(position.x - shape.size.x / 2, position.y - shape.size.y / 2), 
											Vector2(position.x + shape.size.x / 2, position.y - shape.size.y / 2),
											Vector2(position.x + shape.size.x / 2, position.y + shape.size.y / 2),
											Vector2(position.x - shape.size.x / 2, position.y + shape.size.y / 2)])
	add_child(polygon2D)
	print(position)
	print(polygon2D.position)
	print(polygon2D.polygon)
	print(shape.size)
