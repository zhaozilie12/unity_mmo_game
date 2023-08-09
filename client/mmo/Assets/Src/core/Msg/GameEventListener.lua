---@class GameEventListener
local GameEventListener, super = defClass("GameEventListener")
function GameEventListener:ctor(world)
    if GameGlobal and GameGlobal.GameEventListenerIDGenerator then 
        self.listenerID = GameGlobal.GameEventListenerIDGenerator():GenID()
    end
end

function GameEventListener:GetListenerID()
    return self.listenerID
end

---@param gameEventType GameEventType
---@param ... 任意参数
function GameEventListener:OnGameEvent(gameEventType, ...)
end