
var _entityIds = '';
var _networkObjectId = '';
var _tagsAdded = '';
var _tagsRemoved = '';
var _valueDelimiter = ',';
var _isDataValid = true;
var _openAtAnEntityRowLevel = false;
var _preLoadSelectedTags = false;
var _entityType = TagEntities.Asset;
var _tagKey = TagKeys.Tag;
var _preLoadSelectedTags = false;
var _preLoadSelectedTags = false;
var _multiSelectCtrl = undefined;

var _multiSelectTagCtrlDefinition = {
    dataTextField: "TagName",
    dataValueField: "TagId",
    change: function (e) {
        setStatus($('#operationStatus'));
    },
    dataSource: {
        type: "application/json; charset=utf-8",
        transport: {
            read: {
                url: "/tag/GetTagList",
                type: "GET",
                data: { callFromGrid: false,networkObjectId: _networkObjectId, tagKey: _tagKey },
                cache: false
            }
        },
        requestStart: function (e) {
            if (e.type === "read") {
                if (e.sender.transport.options.read.data.networkObjectId == "" || e.sender.transport.options.read.data.networkObjectId == 0)
                {
                    e.preventDefault();
                }
            }
        },
        requestEnd: function(e)
        {
            if (e.type === "read") {
                onmultiSelectTagRequestEnd();
            }
        }
    }
};

$(document).ready(function () {
    initPopUpWindows();
});

//Control: Pop-up Window
function initPopUpWindows() {
    $("#winEntityTags").kendoWindow({
        modal: true,
        width: "500px",
        height: "320px",
        title: "Tags",
        visible: false,
        close: onWinClose,
        activate: function (e) {
            $('#multiselectTag').focus();
        }
    }).data("kendoWindow");
}

function onmultiSelectTagRequestEnd()
{
    if (_preLoadSelectedTags) {
        var inputData = { "entityIds": _entityIds, "entityType": _entityType, "netId": _networkObjectId, tagKey: _tagKey };
        $.ajax({
            url: "/tag/GetTagIdsForEntities",
            type: "GET",
            data: inputData,
            cache: false,
            success: function (data) {
                if(_multiSelectCtrl != undefined)
                    _multiSelectCtrl.data("kendoMultiSelect").value(data.split(_valueDelimiter));
                $("#hdnTag").val(data);
            },
            error: function (xhr, ajaxOptions, thrownError) {
                setStatus($('#operationStatus'), false, thrownError);
            }
        });
    }
}

//Control: kendoMultiSelect for Tags
$(".multiselectTagCtrl").each(function () {
    var tagMultiSelectCrtl = $(this).kendoMultiSelect(_multiSelectTagCtrlDefinition).data("kendoMultiSelect");

    tagMultiSelectCrtl.one("dataBound", function (e) {
       // onmultiSelectTagDatabound();
    });
});
//1. Public function: to open a popup and display Tags of An Entity
function showTagsForAnEntity(entityId, networkObjectId, windowHeaderText) {
    var entityIdArray = new Array();
    entityIdArray.push(entityId);
    showTags(entityIdArray, networkObjectId, windowHeaderText);
    _openAtAnEntityRowLevel = true;
}

//1. Public function: to open a popup and display Tags of one or more selected entitities
function showTagsForSelectedEntities(entityIds, networkObjectId, windowHeaderText) {
    _windowHeaderText = entityIds
    showTags(entityIds, networkObjectId, windowHeaderText);
    _openAtAnEntityRowLevel = false;
}

function showTags(entityIds, networkObjectId, windowHeaderText) {
    _entityIds = JSON.stringify(entityIds);
    _networkObjectId = networkObjectId;
    multiSelectCtrlDataLoad($("#multiselectTag"), networkObjectId);
    setStatus($('#operationStatus'));
    var win = $("#winEntityTags").data("kendoWindow");
    $("#tvSelectedEntity").html("Asset(s) Selected: <b>" + windowHeaderText + "</b>");
    _dataModified = false;
    win.center().open();
}

//1.1 NOTE: parameter 'networkObjectId' is introduced in this method's signature so that 
// method 'multiSelectCtrlDataLoad' can be reused from other pages as well.
function multiSelectCtrlDataLoad(ctrl, networkObjectId, needPreselected, entityType,tagKey) {
    needPreselected = typeof needPreselected !== 'undefined' ? needPreselected : 1;
    entityType = typeof entityType !== 'undefined' ? entityType : TagEntities.Asset;
    tagKey = typeof tagKey !== 'undefined' ? tagKey : TagKeys.Tag;

    if (needPreselected != 0) {
        _preLoadSelectedTags = true;
        _entityType = entityType;
        _networkObjectId = networkObjectId;
        _multiSelectCtrl = ctrl;
        _tagKey = tagKey;
    }
    else
    {
        _preLoadSelectedTags = false;
    }

    var tagMultiSelect = ctrl.data("kendoMultiSelect").dataSource;
    tagMultiSelect.transport.options.read.data.networkObjectId = networkObjectId;
    tagMultiSelect.transport.options.read.data.tagKey = tagKey;
    tagMultiSelect.filter({});
    tagMultiSelect.read();
   
}

//2. Click Event1: saves new tag
$("#btnAddTag").click(function () {
    if (!_isDataValid) {
        return;
    }
    setStatus($('#operationStatus'));
    var text = $("#txtTag").val();
    if (text == undefined || text == null || text == '') {
        return;
    }
    if (!checkTagAlreadyExists()) {
        _tagsAdded = _valueDelimiter + $("#txtTag").val();
        saveTag(1);
    }
    else {
        setStatus($('#operationStatus'), "info", "Tag with this name already exists.");
        return;
    }
});

//2.1
function checkTagAlreadyExists() {
    var tagNameAlreadyExist = false;
    //Ensure the "Tag Name' does not exist already
    var newTagName = $("#txtTag").val();
    if ($("#txtTag").val() != "") {
        var tagMultiSelect = $("#multiselectTag").data("kendoMultiSelect");
        var totalItems = tagMultiSelect.dataSource._data;
        var totalItemsLength = totalItems.length;
        for (var loopCntr = 0; loopCntr < totalItemsLength; loopCntr++) {
            if (totalItems[loopCntr].TagName.toUpperCase() == newTagName.toUpperCase()) {
                //TagName already exists
                tagNameAlreadyExist = true;
                break;
            }
        }
    }
    return tagNameAlreadyExist;
}

//3. Click Event2: saves the Selection Change
$("#btnSaveTag").click(function () {
    getTagsAddedNRemoved();
    saveTag(0);
});

function saveTag(IsNewTag) {
    if (_tagsAdded != "" || _tagsRemoved != "") {
        var requestURL = '';
        if (IsNewTag == 1) {
            requestURL = '/tag/AddNLinkTagToEntities';
        }
        else {
            requestURL = '/tag/LinkTagsToEntities';
        }
        $.ajax({
            url: requestURL,
            type: 'POST',
            dataType: 'json',
            data: { tagIdsAdded: _tagsAdded, tagIdsRemoved: _tagsRemoved, entityIds: _entityIds, enityType: _entityType, networkObjectId: _networkObjectId, tagKey: _tagKey },beforeSend: function (e) {
                $(".savefunctionalbtn").button('loading');
            },
            success: function (data, textStatus, jqXHR) {
                $(".savefunctionalbtn").button('reset');
                multiSelectCtrlDataLoad($("#multiselectTag"), _networkObjectId, 1);
                setStatus($('#operationStatus'), true, data.lastActionResult);
                if (IsNewTag == 1) {
                    $("#txtTag").val("");
                }
                _dataModified = true;
            },
            error: function (xhr, ajaxOptions, thrownError) {
                setStatus($('#operationStatus'), false, thrownError);
            }
        });
    }
}

//4. 
function getTagsAddedNRemoved() {
    //Compare with the initial tag list and determine n hence preserve what changes have been done   
    var multiSelect = $("#multiselectTag").data("kendoMultiSelect");
    var changedTagsVal = multiSelect.value() + "";
    var initialTagsVal = $("#hdnTag").val();

    if (changedTagsVal == "" && initialTagsVal == "") {
        _tagsAdded = "";
        _tagsRemoved = "";
        return;
    }
    else if (initialTagsVal == "") {
        _tagsAdded = _valueDelimiter + changedTagsVal;
        _tagsRemoved = "";
    }
    else if (changedTagsVal == "") {
        _tagsAdded = "";
        _tagsRemoved = _valueDelimiter + initialTagsVal;
    }
    else {
        changedTagsVal = _valueDelimiter + multiSelect.value();
        var initialTagList = initialTagsVal.split(_valueDelimiter);
        var initialTagListLength = initialTagList.length;
        initialTagsVal = _valueDelimiter + initialTagsVal;

        if (_openAtAnEntityRowLevel) {
            if (initialTagListLength > 0) {
                for (var loopCntr = 0; loopCntr < initialTagListLength; loopCntr++) {
                    if (changedTagsVal.indexOf(_valueDelimiter + initialTagList[loopCntr]) != -1) {
                        initialTagsVal = initialTagsVal.replace(_valueDelimiter + initialTagList[loopCntr], '');
                        changedTagsVal = changedTagsVal.replace(_valueDelimiter + initialTagList[loopCntr], '');
                    }
                }
                _tagsAdded = changedTagsVal;
                _tagsRemoved = initialTagsVal;
            }
        }
        else {
            //According to latest business logic, if the pop-up is not opened An Entity Row Level then
            //Then the existing entity-tag links are to be dropped and then added for all the entities.
            _tagsRemoved = initialTagsVal;
            _tagsAdded = changedTagsVal;           
        }
    }
}


//1. TagName must start with a letter a-z A-Z
//2. TagName must be just one word so ' ' character is not allowed
//3. Only dash(-) special character is allowed 
var validateTagName = function (input) {
    setStatus($('#operationStatus'));
    _isDataValid = true;
    var text = input.value;
    if (text == "" || text == null || text == undefined) {
        input.value = "";
        _isDataValid = false;
        return false;
    }
    if (!/[a-zA-Z]/.test(text)) {
        input.value = "";
        _isDataValid = false;
        return false;
    }
    if (/[\~\`\!\@@\#\$\%\^\&\*\(\)\_\+\=\{\}\|\\\/\[\]\:\;\'\"\<\,\>\.\?]/.test(text)) {
        setStatus($('#operationStatus'), false, "Tag name can only have letters(a-z or A-Z), digits(0-9) and dashes(-)");
        _isDataValid = false;
        return false;
    }
    else if (/[\s]/.test(text)) {
        input.value = input.value.trim();
        _isDataValid = true;
        return true;
    }
}

function initChannelmultiselect(channelCtrl,menuId,isNewMenu,entity)
{
    channelCtrl.kendoMultiSelect(_multiSelectTagCtrlDefinition);

    //To reuse Get tags associted for multile entities add menuid to a list
    var entityIds = new Array();
    entityIds.push(menuId);
    //set tagcommon._entityIds to Menu Id to fetch channels associated to Menu
    _entityIds = JSON.stringify(entityIds);

    // 1 - needTagsPreselected for old Menus 0 - New Menus
    var needTagsPreselected = isNewMenu ? 0 : 1;
    multiSelectCtrlDataLoad(channelCtrl, selectedId, needTagsPreselected , entity, TagKeys.Channel);
}