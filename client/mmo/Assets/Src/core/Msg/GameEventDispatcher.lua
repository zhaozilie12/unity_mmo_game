---事件处理分发器
---Dispatch当帧新增事件,延迟到下次分发处理
---Dispatch当帧移除事件,当帧生效
local GameEventDispatcher, super = defClass("GameEventDispatcher")

local tableIKey = table.ikey
local tableClear = table.clear
local tableInsert = table.insert
local fieldGetListenerID = "GetListenerID"
local fieldGetID = "GetID"

function GameEventDispatcher:ctor()
	--当前处理列表
    ---@type table<GameEventType, ArrayList>
    self.type2ListenerContexts = {}
    ---@type table<GameEventType, ArrayList>
    self.type2Callbacks = {}

	--新增的待处理列表
    ---@type table<GameEventType, ArrayList>
	self.type2ListenerContextsTodoList = {}
    ---@type table<GameEventType, ArrayList>
	self.type2CallbacksTodoList = {}

	self.dispatchedIDArray = {}
end

---新增监听(统一回调OnGameEvent)
---@param gameEventType GameEventType
---@param listenerContext GameEventListener
function GameEventDispatcher:AddListener(gameEventType, listenerContext)
    local listenerContexts = self.type2ListenerContextsTodoList[gameEventType]
    if not listenerContexts then
        listenerContexts = ArrayList:New()
        self.type2ListenerContextsTodoList[gameEventType] = listenerContexts
    end

	if self:IsIDInContainer(listenerContexts, listenerContext.listenerID, fieldGetListenerID) then
		return
	end

    listenerContexts:PushFront(listenerContext)
end

---移除监听(统一回调OnGameEvent)
---@param gameEventType GameEventType
---@param listenerID number
function GameEventDispatcher:RemoveListener(gameEventType, listenerID)
	self:RemoveListenerFrom(gameEventType, listenerID, self.type2ListenerContexts,fieldGetListenerID)
	self:RemoveListenerFrom(gameEventType, listenerID, self.type2ListenerContextsTodoList,fieldGetListenerID)
end

---新增监听(回调具体callback)
---@param gameEventType GameEventType
---@param callback Callback
function GameEventDispatcher:AddCallbackListener(gameEventType, callback)
    local listenerCallbacks = self.type2CallbacksTodoList[gameEventType]
    if not listenerCallbacks then
        listenerCallbacks = ArrayList:New()
        self.type2CallbacksTodoList[gameEventType] = listenerCallbacks
    end

	if self:IsIDInContainer(listenerCallbacks, callback:GetID(), fieldGetID) then
		return
	end

    listenerCallbacks:PushFront(callback)
end

---移除监听(回调具体callback)
---@param gameEventType GameEventType
---@param callback Callback
function GameEventDispatcher:RemoveCallbackListener(gameEventType, callback)
    if self.type2Callbacks[gameEventType] and callback then
	    self:RemoveListenerFrom(gameEventType, callback:GetID(), self.type2Callbacks, fieldGetID)
    end
    
    if self.type2CallbacksTodoList[gameEventType] and callback then
	    self:RemoveListenerFrom(gameEventType, callback:GetID(), self.type2CallbacksTodoList, fieldGetID)
    end
end

---@return EventCallback
function GameEventDispatcher:RegisterEventCallBack(gameEventType, clsObject, callback)
    local callBackFunc = GameHelper:GetInstance():CreateEventCallback(gameEventType, callback, clsObject)
    self:AddCallbackListener(gameEventType, callBackFunc)
    return callBackFunc
end
---@param eventCallback EventCallback
function GameEventDispatcher:UnRegisterEventCallback(eventCallback)
    local type = eventCallback:GetEventType()
    self:RemoveCallbackListener(type, eventCallback)
end

---@param gameEventType GameEventType
---@param ... 任意参数
function GameEventDispatcher:Dispatch(gameEventType, ...)
	self:MergeListenersTodoToCurrent(self.type2ListenerContextsTodoList, self.type2ListenerContexts,fieldGetListenerID)
	self:MergeListenersTodoToCurrent(self.type2CallbacksTodoList, self.type2Callbacks,fieldGetID)
	local dispatchedIDArray = tableClear(self.dispatchedIDArray)

    local listenerContexts = self.type2ListenerContexts[gameEventType]
    if listenerContexts then
        for i = listenerContexts:Size(), 1, -1 do
            local listenerContext = listenerContexts:GetAt(i)
            if listenerContext and not tableIKey(dispatchedIDArray, listenerContext.listenerID) then
				tableInsert(dispatchedIDArray, listenerContext.listenerID)
                listenerContext:OnGameEvent(gameEventType, ...)
            end
        end
    end

	tableClear(dispatchedIDArray)
    local listenerCallbacks = self.type2Callbacks[gameEventType]
    if listenerCallbacks then
        for i = listenerCallbacks:Size(), 1, -1 do
            ---@type Callback
            local listenerCallback = listenerCallbacks:GetAt(i)
            if listenerCallback and not tableIKey(dispatchedIDArray, listenerCallback:GetID()) then
				tableInsert(dispatchedIDArray, listenerCallback:GetID())
                listenerCallback:Call(...)
            end
        end
    end
end

function GameEventDispatcher:RemoveAllListeners()
    self.type2ListenerContexts = {}
    self.type2Callbacks = {}
	self.type2ListenerContextsTodoList = {}
	self.type2CallbacksTodoList = {}
end

--region Private
---@private
---@param gameEventType GameEventType
---@param listenerID number
function GameEventDispatcher:RemoveListenerFrom(gameEventType, listenerID, t, checkField)
    local listeners = t[gameEventType]
    if not listeners then
        return
    end

    for i = 1, listeners:Size() do
        local listener = listeners:GetAt(i)
        if listener and listener[checkField](listener) == listenerID then
            listeners:RemoveAt(i)
            break
        end
    end
end

---@private
function GameEventDispatcher:MergeListenersTodoToCurrent(todoTable, curTable, checkField)
	for gameEventType, todoListeners in pairs(todoTable) do
		local curListeners = curTable[gameEventType]
		if curListeners then
			for i=todoListeners:Size(), 1, -1 do
				local todoListener = todoListeners:GetAt(i)
				local todoListenerID = todoListener[checkField](todoListener)
				if not self:IsIDInContainer(curListeners, todoListenerID, checkField) then
					curListeners:PushFront(todoListener)
				end
			end
		else
			curTable[gameEventType] = todoListeners
		end
	end

	tableClear(todoTable)
end

---@private
function GameEventDispatcher:IsIDInContainer(arrayList, targetID, checkField)
	for i = 1, arrayList:Size() do
		local obj = arrayList:GetAt(i)
		if obj and obj[checkField](obj) == targetID then
			return true
		end
	end
	return false
end
--endregion