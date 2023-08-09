--加载

local LoadManager, super = defClassStatic("LoadManager")

LoadManager.LoadingHandlerName = {
    world_map = "WorldMap"
}

function LoadManager:ctor()
    self.loadingHandler = nil
    self.loadingHandlerName = nil
    self.targetLevelName = nil
    self.loadingParams = nil

    self._progressBar = LoadingProgressBar:New()

    self._isCoreGameLoading = false
    self._isLoading = false
end

---@public
---切到新场景的Loading入口
---@param loadingHandlerName string LoadingHandler(处理预加载)
---@param targetLevelName string 目标场景名称(可以为空)
---@param ... 其他loading参数 用在预加载函数中
function LoadingManager:StartLoading(loadingHandlerName, targetLevelName, ...)
    self._isLoading = true
    self._interruptCallback = nil

    self.loadingHandlerName = loadingHandlerName
    if not self.loadingHandlerName then
        return
    end
    ---@type LoadingHandler
    local loadingHandler = _createInstance(self.loadingHandlerName)
    if not loadingHandler then
        return
    end

    if not loadingHandler:IsChildOf("LoadingHandler") then
        Log.fatal(
            "LoadingManager:StartLoading Fail,",
            self.loadingHandlerName,
            " is not inherited from LoadingHandler!"
        )
        return
    end

    loadingHandler:SetProgressBar(self._progressBar)

    self.loadingHandler = loadingHandler
    self.targetLevelName = targetLevelName
    self.loadingParams = {...}
    self._latestHandler = loadingHandlerName --记录最近一次的loading
    self:StartTask(LoadingManager.Load, self)
end

---不能删
function LoadingManager:Update(deltaTimeMS)
end

---@private
function LoadingManager:Load(TT)
    --todo 考虑需不需要清理资源 UnloadUnusedResources
    local tik = os.clock()
    local pm = GameGlobal.GetModule(PetAudioModule)
    pm:StopAll()
    --停止全部UI语音
    AudioHelperController.StopAllUIVoice()

    if GameGlobal.UIStateManager():CurUIStateType() == UIStateType.Invalid or self.loadingHandler:NeedSwitchState() then
        GameGlobal.UIStateManager():SwitchState(
            UIStateType.UICommonLoading,
            self.loadingHandler:LoadingType(),
            self.loadingHandler:LoadingID()
        )
        YIELD(TT)
    else
        local showTaskid =
            GameGlobal.UIStateManager():ShowDialog(
            "UICommonLoading",
            self.loadingHandler:LoadingType(),
            self.loadingHandler:LoadingID()
        )
        JOIN(TT, showTaskid)
    end

    --显示完LoadingUI之后设置进度条
    self._progressBar:Reset()

    --进入Loading场景过渡
    if self.targetLevelName then
        local req = ResourceManager:GetInstance():AsyncLoadAsset(TT, LOADING_SCENE_NAME, LoadType.Unity)
        req:Dispose()
    end

    --预加载资源
    self.loadingHandler:PreLoadBeforeLoadLevel(TT, unpack(self.loadingParams, 1, table.maxn(self.loadingParams)))
    local sceneReq = nil
    if self.targetLevelName then
        YIELD(TT)
        sceneReq = self.loadingHandler:LoadLevel(TT, self.targetLevelName)
    end
    self.loadingHandler:PreLoadAfterLoadLevel(TT, unpack(self.loadingParams, 1, table.maxn(self.loadingParams)))
    GameGlobal.EventDispatcher():Dispatch(GameEventType.LoadLevelEnd, true)
    -- 直接进入UI的loading progress直接设置为100
    if self:IsEnterUI() then
        self._progressBar:Complete()
    -- GameGlobal.EventDispatcher():Dispatch(GameEventType.LoadingProgressChanged, 100)
    end
    local tok = os.clock() - tik
    Log.prof("LoadingManager:Load(TT) use time=", tok * 1000)
end

---@public
---进度条loading的条件以后可能会拓展很多
function LoadingManager:Excute(value)
    ResourceManager:GetInstance():SetSyncLoadNum(0)
    self:onLoadingFinish()
end

---@private
function LoadingManager:StartTask(func, ...)
    TaskManager:GetInstance():StartTask(func, ...)
end

function LoadingManager:IsEnterUI()
    return EnterUILoadingHandler[self.loadingHandlerName]
end

function LoadingManager:IsLoading()
    return self._isLoading or self._isCoreGameLoading
end

--打断Loading
function LoadingManager:Interrupt(cb)
    if not self:IsLoading() then
        Log.warn("not loading now,cant interrupt")
        return
    end

    if self._interruptCallback and not cb then
        Log.warn("cant set interrupt callback nil")
        return
    end

    if self._isCoreGameLoading then
        Log.debug("[Loading] 尝试打断局内loading")
    end

    if cb then
        GameGlobal.UIStateManager():Lock("WaitToInterruptLoading")
        GameGlobal.UIStateManager():ShowBusy(true)
        self._interruptCallback = cb
    end
end

--局内走特殊的Loading，需要通知LoadingManager
function LoadingManager:CoreGameLoadingStart()
    self._isCoreGameLoading = true
end

function LoadingManager:CoreGameLoadingFinish()
    Log.debug("[Loading] 局内loading结束")
    self:onLoadingFinish()
    self._isCoreGameLoading = false
end

function LoadingManager:onLoadingFinish()
    self._isLoading = false

    if self._interruptCallback then
        local cb = self._interruptCallback
        self._interruptCallback = nil
        GameGlobal.UIStateManager():UnLock("WaitToInterruptLoading")
        GameGlobal.UIStateManager():ShowBusy(false)
        if self._isCoreGameLoading then
            Log.debug("[Loading] 执行局内打断回调")
        else
            Log.debug("[Loading] 执行普通打断回调")
        end
        cb()
        return
    end

    if self.loadingHandler then
        self.loadingHandler:LoadingFinish(unpack(self.loadingParams, 1, table.maxn(self.loadingParams)))
    end
    self.loadingHandler = nil
    self.loadingHandlerName = nil
    self.targetLevelName = nil
    self.loadingParams = nil
end

----------------------------------
--[[
    Loading进度条
]]
---@class LoadingProgressBar:Object
_class("LoadingProgressBar", Object)
LoadingProgressBar = LoadingProgressBar

function LoadingProgressBar:Constructor()
    self._percent = 0
end

--设置进度0-100
function LoadingProgressBar:SetProgress(progress)
    progress = Mathf.Clamp(progress, 0, 100)
    if self._percent > progress then
        Log.fatal("Loading progress error:", progress, "，current is ", self._percent)
        return
    elseif self._percent == progress then
        return
    end
    self._percent = progress
    GameGlobal.EventDispatcher():Dispatch(GameEventType.LoadingProgressChanged, self._percent)
end

--获取当前进度
function LoadingProgressBar:GetProgress()
    return self._percent
end

function LoadingProgressBar:Reset()
    self._percent = -1
    self:SetProgress(0)
end

function LoadingProgressBar:Complete()
    self:SetProgress(100)
end
