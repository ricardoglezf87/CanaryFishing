local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")

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

local RodConfig = require(modules:WaitForChild("RodConfig"))
local FishingMath = require(modules:WaitForChild("FishingMath"))

local activeCasts: {[Player]: {part: BasePart, origin: Vector3}} = {}

local function clearCast(player: Player)
	local cast = activeCasts[player]
	if cast and cast.part then
		cast.part:Destroy()
	end
	activeCasts[player] = nil
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

	activeCasts[player] = {part = lure, origin = origin}

	task.delay(12, function()
		if activeCasts[player] and activeCasts[player].part == lure then
			clearCast(player)
		end
	end)
end)

reelRequest.OnServerEvent:Connect(function(player: Player)
	clearCast(player)
end)

Players.PlayerRemoving:Connect(clearCast)
