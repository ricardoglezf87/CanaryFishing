local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local ServerStorage = game:GetService("ServerStorage")
local RunService = game:GetService("RunService")

local fishingSystem = ReplicatedStorage:WaitForChild("FishingSystem")
local modules = fishingSystem:WaitForChild("Modules")
local remotes = fishingSystem:FindFirstChild("Remotes") or Instance.new("Folder")
remotes.Name = "Remotes"
remotes.Parent = fishingSystem

local castRequest = remotes:FindFirstChild("CastRequest") or Instance.new("RemoteEvent")
castRequest.Name = "CastRequest"
castRequest.Parent = remotes

local reelRequest = remotes:FindFirstChild("ReelRequest") or Instance.new("RemoteEvent")
reelRequest.Name = "ReelRequest"
reelRequest.Parent = remotes

local fishingState = remotes:FindFirstChild("FishingState") or Instance.new("RemoteEvent")
fishingState.Name = "FishingState"
fishingState.Parent = remotes

local RodConfig = require(modules:WaitForChild("RodConfig"))
local FishingMath = require(modules:WaitForChild("FishingMath"))

local activeCasts: {[Player]: {part: BasePart, origin: Vector3, status: string, tension: number, progress: number, reeling: boolean, biteAt: number?}} = {}

local function createRodTemplate(): Tool
	local existing = ServerStorage:FindFirstChild("BasicRod")
	if existing and existing:IsA("Tool") then return existing end
	local tool = Instance.new("Tool")
	tool.Name = "BasicRod"
	tool.RequiresHandle = true
	tool.CanBeDropped = false
	local handle = Instance.new("Part")
	handle.Name = "Handle"
	handle.Size = Vector3.new(0.12, 2.8, 0.12)
	handle.Color = Color3.fromRGB(95, 55, 25)
	handle.Parent = tool
	tool.Parent = ServerStorage
	return tool
end

local function setupPod()
	local pod = workspace:FindFirstChild("FishingPod")
	if not pod or not pod:IsA("BasePart") then
		warn("FishingPod no existe en Workspace")
		return
	end
	local prompt = pod:FindFirstChildOfClass("ProximityPrompt") or Instance.new("ProximityPrompt")
	prompt.ActionText = "Coger caña"
	prompt.ObjectText = "Soporte de pesca"
	prompt.KeyboardKeyCode = Enum.KeyCode.E
	prompt.HoldDuration = 0.25
	prompt.Parent = pod
	prompt.Triggered:Connect(function(player: Player)
		local character = player.Character
		local humanoid = character and character:FindFirstChildOfClass("Humanoid")
		local backpack = player:FindFirstChildOfClass("Backpack")
		if not humanoid or not backpack then return end
		if character:FindFirstChild("BasicRod") or backpack:FindFirstChild("BasicRod") then return end
		local rod = createRodTemplate():Clone()
		rod.Parent = backpack
		humanoid:EquipTool(rod)
	end)
end

local function isPointInsideWater(point: Vector3): boolean
	local water = workspace:FindFirstChild("FishingWater")
	if not water or not water:IsA("BasePart") then return false end
	local p = water.CFrame:PointToObjectSpace(point)
	local half = water.Size * 0.5
	return math.abs(p.X) <= half.X and math.abs(p.Z) <= half.Z and p.Y >= -half.Y - 2 and p.Y <= half.Y + 4
end

setupPod()

local function clearCast(player: Player, message: string?)
	local cast = activeCasts[player]
	if cast and cast.part then
		cast.part:Destroy()
	end
	activeCasts[player] = nil
	if message then
		fishingState:FireClient(player, "Reset", message)
	end
end

local function getCastOrigin(player: Player): (Vector3?, Vector3?)
	local character = player.Character
	if not character then
		return nil, nil
	end

	local head = character:FindFirstChild("Head")
	if not head or not head:IsA("BasePart") then
		return nil, nil
	end

	return head.Position + (head.CFrame.LookVector * 1.5), head.CFrame.LookVector
end

castRequest.OnServerEvent:Connect(function(player: Player, chargeAlpha: number)
	if not player.Character or not player.Character:FindFirstChild("BasicRod") then return end
	if typeof(chargeAlpha) ~= "number" then
		return
	end

	local origin, lookVector = getCastOrigin(player)
	if not origin or not lookVector then
		return
	end

	clearCast(player)

	local lure = Instance.new("Part")
	lure.Name = player.Name .. "_Lure"
	lure.Shape = Enum.PartType.Ball
	lure.Size = Vector3.new(0.35, 0.35, 0.35)
	lure.Color = Color3.fromRGB(255, 170, 0)
	lure.Material = Enum.Material.Neon
	lure.CanCollide = true
	lure.Position = origin
	lure.Parent = workspace

	lure:SetNetworkOwner(nil)
	lure.AssemblyLinearVelocity = FishingMath.getCastVelocity(
		math.clamp(chargeAlpha, 0, 1),
		RodConfig.Cast.MinPower,
		RodConfig.Cast.MaxPower,
		lookVector,
		RodConfig.Cast.UpwardAngle
	)

	activeCasts[player] = {part = lure, origin = origin, status = "Flying", tension = 0, progress = 0, reeling = false}
	fishingState:FireClient(player, "Cast", "El señuelo está en el agua...")

	task.delay(12, function()
		if activeCasts[player] and activeCasts[player].part == lure then
			clearCast(player)
		end
	end)
end)

reelRequest.OnServerEvent:Connect(function(player: Player, isReeling: boolean)
	local cast = activeCasts[player]
	if cast then cast.reeling = isReeling == true end
end)

RunService.Heartbeat:Connect(function(deltaTime)
	for player, cast in pairs(activeCasts) do
		if not cast.part or not cast.part.Parent then
			clearCast(player)
		elseif cast.status == "Flying" and isPointInsideWater(cast.part.Position) then
			cast.status = "WaitingForBite"
			cast.biteAt = os.clock() + math.random(2, 5)
			fishingState:FireClient(player, "WaitingForBite", "Esperando una picada...")
		elseif cast.status == "WaitingForBite" and cast.biteAt and os.clock() >= cast.biteAt then
			cast.status = "Hooked"
			cast.tension = 0.25
			cast.progress = 0
			fishingState:FireClient(player, "Bite", "¡Picada! Mantén la tensión controlada")
		elseif cast.status == "Hooked" then
			if cast.reeling then
				cast.tension = math.clamp(cast.tension + 0.15 * deltaTime, 0, 1)
				cast.progress = math.clamp(cast.progress + 0.35 * deltaTime, 0, 1)
			else
				cast.tension = math.clamp(cast.tension - 0.25 * deltaTime, 0, 1)
			end
			fishingState:FireClient(player, "Tension", cast.tension)
			if cast.progress >= 1 then
				clearCast(player, "¡Capturaste un pez!")
			elseif cast.tension >= 1 then
				clearCast(player, "¡La línea se ha roto!")
			end
		end
	end
end)

Players.PlayerRemoving:Connect(clearCast)
