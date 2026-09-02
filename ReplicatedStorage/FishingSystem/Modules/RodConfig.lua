-- Configuración de la caña inicial.
-- Este módulo debe estar dentro de ReplicatedStorage/FishingSystem/Modules.

local RodConfig = {
	Id = "basic_rod",
	Name = "Caña básica",
	Cast = {
		MinPower = 38,
		MaxPower = 105,
		ChargeTime = 1.25,
		UpwardAngle = 0.28,
	},
	Line = {
		MaxLength = 120,
		BreakStrength = 18,
	},
}

return RodConfig
