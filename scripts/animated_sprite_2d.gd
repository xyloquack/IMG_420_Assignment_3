extends AnimatedSprite2D

var time_passed
var direction
@export var speed: float
var velocity: float

func _ready():
	time_passed = 0
	direction = 1
	velocity = 0
	
func _process(delta):
	time_passed += delta
	if time_passed > 3:
		time_passed -= 3
		direction = -direction
	
	velocity = lerp(velocity, speed * direction, 0.2)
	position += Vector2(velocity * delta, 0)
	if velocity < 0:
		flip_h = true
	if velocity > 0:
		flip_h = false
