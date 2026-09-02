local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local UserInputService = game:GetService("UserInputService")
local RunService = game:GetService("RunService")

local player = Players.LocalPlayer
local playerGui = player:WaitForChild("PlayerGui")

-- El prototipo usa primera persona desde el inicio.
player.CameraMode = Enum.CameraMode.LockFirstPerson
UserInputService.MouseBehavior = Enum.MouseBehavior.LockCenter
UserInputService.MouseDeltaSensitivity = 0.18

local fishingSystem = ReplicatedStorage:WaitForChild("FishingSystem")
local remotes = fishingSystem:WaitForChild("Remotes")
local castRequest = remotes:WaitForChild("CastRequest")
local reelRequest = remotes:WaitForChild("ReelRequest")
local fishingState = remotes:WaitForChild("FishingState")

local gui = Instance.new("ScreenGui")
gui.Name = "FishingHud"
gui.ResetOnSpawn = false
gui.Parent = playerGui

local powerFrame = Instance.new("Frame")
powerFrame.Name = "CastPower"
powerFrame.AnchorPoint = Vector2.new(0.5, 1)
powerFrame.Position = UDim2.fromScale(0.5, 0.9)
powerFrame.Size = UDim2.fromOffset(260, 24)
powerFrame.BackgroundColor3 = Color3.fromRGB(35, 35, 35)
powerFrame.Visible = false
powerFrame.Parent = gui

local powerFill = Instance.new("Frame")
powerFill.Name = "Fill"
powerFill.Size = UDim2.fromScale(0, 1)
powerFill.BackgroundColor3 = Color3.fromRGB(70, 210, 100)
powerFill.Parent = powerFrame

local label = Instance.new("TextLabel")
label.BackgroundTransparency = 1
label.Position = UDim2.fromOffset(0, -28)
label.Size = UDim2.fromScale(1, 1)
label.TextColor3 = Color3.new(1, 1, 1)
label.Text = "Mantén pulsado para cargar"
label.Parent = powerFrame

local charging = false
local chargeStartedAt = 0
local chargeTime = 1.25

local function hasRod(): boolean
	local character = player.Character
	return character ~= nil and character:FindFirstChild("BasicRod") ~= nil
end

local function setPower(alpha: number)
	powerFill.Size = UDim2.fromScale(alpha, 1)
	powerFill.BackgroundColor3 = Color3.fromHSV((1 - alpha) * 0.33, 0.85, 1)
end

UserInputService.InputBegan:Connect(function(input, processed)
	if processed or not hasRod() then return end
	if input.UserInputType == Enum.UserInputType.MouseButton1 then
	if charging then
		return
	end

	charging = true
	chargeStartedAt = os.clock()
	powerFrame.Visible = true
	label.Text = "Suelta para lanzar"
elseif input.KeyCode == Enum.KeyCode.R then
	reelRequest:FireServer(true)
end
end)

UserInputService.InputEnded:Connect(function(input)
	if input.UserInputType ~= Enum.UserInputType.MouseButton1 then
		if input.KeyCode == Enum.KeyCode.R then reelRequest:FireServer(false) end
		return
	end
	if not charging then
		return
	end

	local alpha = math.clamp((os.clock() - chargeStartedAt) / chargeTime, 0, 1)
	charging = false
	powerFrame.Visible = false
	setPower(0)
	if hasRod() then castRequest:FireServer(alpha) end
end)

fishingState.OnClientEvent:Connect(function(state, value)
	if state == "Tension" then
		powerFrame.Visible = true
		powerFill.Size = UDim2.fromScale(math.clamp(value, 0, 1), 1)
		powerFill.BackgroundColor3 = Color3.fromHSV((1 - value) * 0.33, 0.9, 1)
		label.Text = "Tensión: " .. math.floor(value * 100) .. "% | Mantén R para luchar"
	elseif state == "Reset" then
		powerFrame.Visible = false
		label.Text = value or ""
	else
		label.Text = value or ""
		if state == "Bite" then powerFrame.Visible = true end
	end
end)

RunService.RenderStepped:Connect(function()
	if charging then
		local alpha = math.clamp((os.clock() - chargeStartedAt) / chargeTime, 0, 1)
		setPower(alpha)
	end
end)
