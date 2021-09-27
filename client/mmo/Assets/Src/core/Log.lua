--@func Log.set(enable, detail)
--@desc 设置Log开启状态和显示级别
--@arg  enable:table, 是否输出内容{true/false(Log.i是否输出),true/false(Log.w是否输出),true/false(Log.e是否输出)}
--@arg  detail:table, 是否输出堆栈{true/false(Log.i是否输出),true/false(Log.w是否输出),true/false(Log.e是否输出)}

--@func Log.i(...)
--@desc 输出Info信息

--@func Log.w(...)
--@desc 输出Warn信息

--@func Log.e(...)
--@desc 输出Error信息

------------------------------------

local Log, super = defClassStatic("Log")

local fLog = UnityEngine.Debug.Log
local fLogError = UnityEngine.Debug.LogError
local fLogWarning = UnityEngine.Debug.LogWarning

Log._enable = {true, true, true}
Log._detail = {false, false, true}
Log._actmap = {}

Log._i = function(...)
	local arg = table.pack(...)
	local str = "[I]"
	for i = 1, arg.n do
		str = str .. tostring(arg[i]) .. "\t"
	end

	if Log._detail[1] then
		str = str .. "\n" .. Log._info()
	end
    
	fLog(str)
end

Log._w = function(...)
	local arg = table.pack(...)
	local str = "[W]"
	for i = 1, arg.n do
		str = str .. tostring(arg[i]) .. "\t"
	end

	if Log._detail[2] then
		str = str .. "\n" .. Log._info()
	end
	
	fLogWarning(str)
end

Log._e = function(...)
	local arg = table.pack(...)
	local str = "[E]"
	for i = 1, arg.n do
		str = str .. tostring(arg[i]) .. "\t"
	end

	if Log._detail[3] then
		str = str .. Log._info()
	end
	
	fLogError(str)
end

Log._void = function()
	--void
end

Log._info = function()
	return debug.traceback("", 2)
end

Log.i = Log._void
Log.w = Log._void
Log.e = Log._e

Log.act = function(str)
    table.insert(Log._actmap, str)
    if #Log._actmap >= 64 then
        table.move(Log._actmap, 32, 96, 1)
    end
end

Log.map = function()
    return table.concat(Log._actmap, "\n")
end

Log.set = function(enable, detail)
	Log._enable = enable
	Log._detail = detail
	
	if enable[1] then
		Log.i = Log._i
	else
		Log.i = Log._void
	end
	
	if enable[2] then
		Log.w = Log._w
	else
		Log.w = Log._void
	end
	
	if enable[3] then
		Log.e = Log._e
	else
		Log.e = Log._void
	end
end





