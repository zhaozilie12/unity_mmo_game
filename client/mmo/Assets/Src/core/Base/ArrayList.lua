--[[------------------------------------------------------------------------------------------
**********************************************************************************************
    容器：维护插入顺序的数组
    相当于C++的std::vector，Java/C#的ArrayList
**********************************************************************************************
]] --------------------------------------------------------------------------------------------
local ArrayList, super = defClass("ArrayList")
ArrayList = ArrayList
function ArrayList:Constructor()
    self.elements = {}
end

function ArrayList:Empty()
    return #self.elements == 0
end

function ArrayList:Size()
    return #self.elements
end

function ArrayList:Clear()
    self.elements = {}
end

--[[-------------------------------------------
    把value插在数组末尾
    检查nil是为了维护好索引的连续性
]]---------------------------------------------
function ArrayList:PushBack(value)
    if value == nil then
        return
    end
    local elements = self.elements
    elements[#elements + 1] = value
end

--[[-------------------------------------------
    把value插在数组开头
    检查nil是为了维护好索引的连续性
]]---------------------------------------------
function ArrayList:PushFront(value)
    if value == nil then
        return
    end
    local elements = self.elements
    for i = #elements, 1, -1 do
        elements[i + 1] = elements[i]
    end
    elements[1] = value
end
---------------------------------------------

--[[-------------------------------------------
    把value插入在索引index指定的位置
    检查nil是为了维护好索引的连续性
]]---------------------------------------------
function ArrayList:Insert(value, index)
    if value == nil then
        return
    end
    local elements = self.elements
    local size = #elements
    if index < 1 then
        self:PushFront(value)
        return
    elseif index > size then
        self:PushBack(value)
        return
    end
    for i = size, index, -1 do
        elements[i + 1] = elements[i]
    end
    elements[index] = value
end

--[[-------------------------------------------
    把索引index处的元素挪到数组末尾
]]---------------------------------------------
function ArrayList:MoveToBack(index)
    local elements = self.elements
    local size = #elements
    if index < 1 or index >= size then
        return
    end
    local temp = elements[index]
    for i = index, size - 1 do
        elements[i] = elements[i + 1]
    end
    elements[size] = temp
end
---------------------------------------------

--[[-------------------------------------------
    把索引index处的元素挪到数组开头
]]---------------------------------------------
function ArrayList:MoveToFront(index)
    local elements = self.elements
    local size = #elements
    if index <= 1 or index > size then
        return
    end
    local temp = elements[index]
    for i = index - 1, 1, -1 do
        elements[i + 1] = elements[i]
    end
    elements[1] = temp
end

--[[-------------------------------------------
    把索引old_index处的元素挪到索引new_index指定的位置
]]---------------------------------------------
function ArrayList:MoveToIndex(old_index, new_index)
    local elements = self.elements
    local size = #elements
    if old_index < 1 or old_index > size then
        return
    end
    if new_index < 1 or new_index > size then
        return
    end
    if old_index == new_index then
        return
    end
    local temp = elements[old_index]
    if old_index < new_index then
        for i = old_index, new_index - 1 do
            elements[i] = elements[i + 1]
        end
    else
        for i = old_index - 1, new_index, -1 do
            elements[i + 1] = elements[i]
        end
    end
    elements[new_index] = temp
end

--[[-------------------------------------------
    移除并返回数组末尾的元素
]]---------------------------------------------
function ArrayList:PopBack()
    local elements = self.elements
    local size = #elements
    local temp = elements[size]
    elements[size] = nil
    return temp
end
---------------------------------------------

--[[-------------------------------------------
    删除索引index处的元素
]]---------------------------------------------
function ArrayList:RemoveByIndex(index)
    local elements = self.elements
    local size = #elements
    if index < 1 or index > size then
        return
    end
    for i = index, size - 1 do
        elements[i] = elements[i + 1]
    end
    elements[size] = nil
end
---------------------------------------------

--[[-------------------------------------------
    RemoveAt等同于RemoveByIndex
]]---------------------------------------------
ArrayList.RemoveAt = ArrayList.RemoveByIndex

--[[-------------------------------------------
    从数组开头遍历搜索第一个值为value的元素，如果找到则删除
]]---------------------------------------------
function ArrayList:RemoveFirst(value)
    local index = self:Find(value, 1)
    self:RemoveAt(index)
    return index
end

--[[-------------------------------------------
    Remove等同于RemoveFirst
]]---------------------------------------------
ArrayList.Remove = ArrayList.RemoveFirst

--[[-------------------------------------------
    从索引from_index开始遍历搜索第一个值为value的元素，如果找到则返回索引
]]---------------------------------------------
function ArrayList:Find(value, from_index)
    if not from_index then
        from_index = 1
    end
    local elements = self.elements
    for i = from_index, #elements do
        if elements[i] == value then
            return i
        end
    end
    return -1
end

---若数组里存放的是简单的值类型，比如number，string这种，可以调用该函数；如果是table，建议不要调用该函数，自己遍历，通过比较table的某个属性，比如通过比较ID
function ArrayList:Contains(value, from_index)
    local index = self:Find(value, from_index)
    if index == -1 then
        return false
    else
        return true
    end
end
---------------------------------------------

--[[-------------------------------------------
    返回索引index处的元素
]]---------------------------------------------
function ArrayList:GetAt(index)
    return self.elements[index]
end

--[[-------------------------------------------
    返回第一个元素
]]---------------------------------------------
function ArrayList:Front()
    return self.elements[1]
end

--[[-------------------------------------------
    对所有元素依次调用函数func
]]---------------------------------------------
function ArrayList:ForEach(func)
    local elements = self.elements
    for i = 1, #elements do
        func(elements[i])
    end
end

function ArrayList:HandleForeach(handler, func, ...)
    local elements = self.elements
    for i = 1, #elements do
        local bSuccess = func(handler, elements[i], ...)
        if false == bSuccess or 0 == bSuccess then
            break
        end
    end
end
---2019-10-25 韩玉信添加
function ArrayList:RemoveByValue(value)
    local index = self:Find(value, 1)
    if index < 1 or index > #self.elements then
        return false;
    end
    self:RemoveAt(index)
    return true;
end
---@param al ArrayList
function ArrayList:Clone(al)
    if al == nil then return end
    if type(al) ~= "table" then return end
    if al._className ~= "ArrayList" then
        return
    end
    
    al:ForEach(function(v)
        self:PushBack(v)
    end)
end