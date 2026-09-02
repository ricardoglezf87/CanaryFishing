local FishingMath = {}

function FishingMath.getChargeAlpha(heldTime: number, chargeTime: number): number
	if chargeTime <= 0 then
		return 1
	end

	return math.clamp(heldTime / chargeTime, 0, 1)
end

function FishingMath.getCastVelocity(alpha: number, minPower: number, maxPower: number, lookVector: Vector3, upwardAngle: number): Vector3
	local power = minPower + ((maxPower - minPower) * math.clamp(alpha, 0, 1))
	local direction = (lookVector + Vector3.new(0, upwardAngle, 0)).Unit

	return direction * power
end

function FishingMath.isWithinCastRange(origin: Vector3, target: Vector3, maxLength: number): boolean
	return (target - origin).Magnitude <= maxLength
end

return FishingMath
