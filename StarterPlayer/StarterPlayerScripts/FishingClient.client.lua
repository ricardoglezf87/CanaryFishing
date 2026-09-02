local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")

local player = Players.LocalPlayer
local playerGui = player:WaitForChild("PlayerGui")
local mouse = player:GetMouse()
local keyboard = game:GetService("UserInputService")

-- El prototipo usa primera persona desde el inicio.
player.CameraMode = Enum.CameraMode.LockFirstPerson

local fishingSystem = ReplicatedStorage:WaitForChild("FishingSystem")
local remotes = fishingSystem:WaitForChild("Remotes")
local castRequest = remotes:WaitForChild("CastRequest")
local reelRequest = remotes:WaitForChild("ReelRequest")

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

local function setPower(alpha: number)
	powerFill.Size = UDim2.fromScale(alpha, 1)
	powerFill.BackgroundColor3 = Color3.fromHSV((1 - alpha) * 0.33, 0.85, 1)
end

mouse.Button1Down:Connect(function()
	if charging then
		return
	end

	charging = true
	chargeStartedAt = os.clock()
	powerFrame.Visible = true
	label.Text = "Suelta para lanzar"
end)

mouse.Button1Up:Connect(function()
	if not charging then
		return
	end

	local alpha = math.clamp((os.clock() - chargeStartedAt) / chargeTime, 0, 1)
	charging = false
	powerFrame.Visible = false
	setPower(0)
	castRequest:FireServer(alpha)
end)

keyboard.InputBegan:Connect(function(key, gameProcessed)
	if(gameProcessed) then 
		return
	end
	
	if key.KeyCode == Enum.KeyCode.R then
		reelRequest:FireServer()
	end
end)

game:GetService("RunService").RenderStepped:Connect(function()
	if charging then
		local alpha = math.clamp((os.clock() - chargeStartedAt) / chargeTime, 0, 1)
		setPower(alpha)
	end
end)
