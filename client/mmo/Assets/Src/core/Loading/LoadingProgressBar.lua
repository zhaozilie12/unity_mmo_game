local LoadingProgressBar, super = defClass("LoadingProgressBar")

function LoadingProgressBar:Constructor()
    self._percent = 0
end

--设置进度0-100
function LoadingProgressBar:SetProgress(progress)
    progress = UnityEngine.Mathf.Clamp(progress, 0, 100)
    if self._percent > progress then
        return
    elseif self._percent == progress then
        return
    end
    self._percent = progress
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
