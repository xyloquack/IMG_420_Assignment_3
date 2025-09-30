extends CharacterBody2D

@export var speed: float
@export var acceleration: float
@export var air_acceleration: float
@export var friction: float
@export var air_friction: float
@export var max_jump_time: float
@export var jump_speed: float
@export var gravity: float

func _ready():
	velocity = Vector2.ZERO
	
