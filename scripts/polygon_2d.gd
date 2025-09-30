extends Polygon2D

var time_passed

func _ready():
	time_passed = 0

func _process(delta):
	time_passed += delta
	rotation = time_passed
