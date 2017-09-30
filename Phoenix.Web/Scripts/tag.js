
var _IsNewTagToAdd = false;
var _tree = null;
var _networkObjectType = null;
var _isRecheckForUniquenessNeeded = null;
var _isRecheckForUniquenessCompleted = false
var _isRecheckForUniquenessResult = null;
var valueDelimiter = "^`";

_expansionSequence = sessionStorage.ExpansionSequence_BrandLevel != undefined && sessionStorage.ExpansionSequence_BrandLevel != "" ? sessionStorage.ExpansionSequence_BrandLevel.split(',') : new Array();
_initialExpansionAfterSelectionItemType = _initialExpansionItemType = NetworkObjectType.Root; //This is the @@Model.ItemType

$(document).ready(function () {
    highlightMenubar("menu", "tag");
    setStatus($('#operationStatus'));
    var _root = new kendo.data.HierarchicalDataSource({
        transport: {
            read: {
                cache: false,
                url: "/site/BrandLeveTree",
                dataType: "json",
                data: { 'networkObjectType': _initialExpansionItemType }
            }
        },
        schema: {
            model: {
                id: "Id",
                hasChildren: "HasChildren",
                expanded: "expanded",
                parentId: "parentId",
                ItemType: "ItemType"
            }
        }
    });

    _tree = $("#treeView").kendoTreeView({
        dataSource: _root,
        dataTextField: "Name",
        dataImageUrlField: "Image",
        loadOnDemand: true,
        dataBound: function (e) {
            handleTreeDataBound(e);
        },
        select: function (e) {
            handleTreeNodeSelection(e.node);
        }
    });

    treeViewCtrl = $("#treeView").data("kendoTreeView");
});

//tag delete
$("#btnTagDelete").click(function () {
    setStatus($('#operationStatus'));

    var grid = $("#gridTag").data("kendoGrid");

    var tagsChecked = grid.tbody.find(":checkbox:checked");
    var tagsCheckedCnt = tagsChecked.length;

    if (tagsCheckedCnt == 0) {
        statusMessage("Info", "Select one or more tags to delete");
        return;
    }

    var tagList = new Array();
    tagsChecked.each(function () {
        tagList.push(grid.dataItem($(this).closest("tr")));
    });
    confirmWindow("Delete confirm", "Are you sure you want to delete the selected tag(s)?", function () { }, "400px", "OK", "Cancel", function (data) {
        if (data === true) {
            deleteTags(tagList);
        }
    });
});

//refresh Tags
function refreshTags() {
    var grid = $("#gridTag").data("kendoGrid").dataSource;
    grid.transport.options.read.data.networkObjectId = selectedId;
    grid.page(1);
}

function onGridDataBound(e) {
    var grid = $("#gridTag").data("kendoGrid");
    if (grid != undefined) {
        var taglist = grid.dataSource._data;
        var tagListCount = taglist.length;
        var taglistString = "";
        if (tagListCount > 0) {
            //1. store Complete Tag List
            for (var loopCntr = 0; loopCntr < tagListCount; loopCntr++) {
                taglistString += valueDelimiter + taglist[loopCntr].TagId + valueDelimiter + taglist[loopCntr].TagName.toUpperCase();
            }
            //1.1
            sessionStorage.GridData = taglistString + valueDelimiter;

            //2. Disable 'Edit' and 'Delete' buttons if the Tag is an inherited tag.
            disableActionsForInheritedTags(e);
        }
        this.thead.find(":checkbox")[0].checked = false;
    }
    triggerResizeGrid();
}

function disableActionsForInheritedTags(e) {
    var grid = e.sender;
    grid.tbody.find("tr .k-grid-edit").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        //Check in the current dataItem if the row is editable
        if (currentDataItem.IsInheritedTag === true) {
            $(this).remove();
        }

    });
    grid.tbody.find("tr .k-grid-deleteTag").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        //Check in the current dataItem if the row is editable
        if (currentDataItem.IsInheritedTag === true) {
            $(this).remove();
        }

    });
    grid.tbody.find(":checkbox").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        //Check in the current dataItem if the row is editable
        if (currentDataItem.IsInheritedTag === true) {
            this.setAttribute('disabled', 'disabled');
        }

    });
}

// Tag delete
function deleteTag(e) {
    e.preventDefault();
    var dataItem = this.dataItem($(e.currentTarget).closest("tr"));

    var confirmationMsg = "Are you sure you want to delete the tag: '" + dataItem.TagName + "' ?";

    if (dataItem.HasAssociatedData) {
        confirmationMsg = "This tag: '" + dataItem.TagName + "' has got some associated data.";
        confirmationMsg += "<br/> Are you sure you want to delete this tag?";
    }

    confirmWindow("Delete confirm", confirmationMsg, function () { }, "400px", "OK", "Cancel", function (data) {
        if (data === true) {
            //This approach is followed to make use of existing methods: deleteTags & '/tag/DeleteTags' methods
            var selectedIds = new Array();
            selectedIds.push(dataItem);
            deleteTags(selectedIds);
        }
    });
}

function deleteTags(tagList) {
    $.ajax({
        url: '/tag/DeleteTags',
        type: 'POST',
        dataType: 'json',
        data: { selectedIds: JSON.stringify(tagList), networkObjectId: selectedId, tagKey: _tagKey },
        success: function (data, textStatus, jqXHR) {
            refreshTags();
            statusMessage(true, data.lastActionResult);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            statusMessage(false, thrownError);
        }
    });
}

function detailInit(e) {
    if (e.data.TagId == null)
        return;

    var detailRow = e.detailRow;
    try {
        $.ajax({
            url: '/tag/GetTagAssociatedData',
            type: 'GET',
            dataType: 'json',
            data: { tagId: e.data.TagId, networkObjectId: selectedId, tagKey: _tagKey },
            success: function (data, textStatus, jqXHR) {
                if (data.status) {
                    var liAssetsCtrl = $(detailRow).find("#liAssets");
                    var liItemsCtrl = $(detailRow).find("#liItems");

                    liAssetsCtrl.html(liAssetsCtrl.html() + data.associatedData["assets"]);
                    liItemsCtrl.html(liItemsCtrl.html() + data.associatedData["items"]);
                }
                else {
                    statusMessage(data.status, data.msg);
                }
            },
            error: function (xhr, ajaxOptions, thrownError) {
                statusMessage(false, thrownError);
            }

        });
    }
    catch (e) {
        statusMessage(false, e);
    }
}

function checkForUniquenessOfTagName(enteredTagName, rowId) {
    _isRecheckForUniquenessNeeded = false;
    var returnValue = false;
    var tagList = sessionStorage.GridData;
    //Ignore the substring which is with same RowId
    var subStrWithRowId = valueDelimiter + rowId + valueDelimiter;
    var subStrWithRowIdCharLength = subStrWithRowId.length;

    var indexOfSubStrWithRowId = tagList.indexOf(subStrWithRowId);
    var indexOfFollowingValueDelimiter = tagList.indexOf(valueDelimiter, indexOfSubStrWithRowId + subStrWithRowIdCharLength);
    var subStringVd_Id_Vd_Name = tagList.substring(indexOfSubStrWithRowId, indexOfFollowingValueDelimiter);
    tagList = tagList.replace(subStringVd_Id_Vd_Name, '');//Vd is ValueDelimiter
    tagList += valueDelimiter;
    returnValue = tagList.indexOf(valueDelimiter + enteredTagName.toUpperCase() + valueDelimiter) == -1;
    if (returnValue) {
        if (_networkObjectType != undefined && _networkObjectType == NetworkObjectType.Root) {
            _isRecheckForUniquenessNeeded = true;
        }
    }

    return returnValue;
}

function restoreSelectionSub() {
    try {
        selectedId = sessionStorage.SelectedTreeNodeId;
    }
    catch (e) {
        alert(e);
    }
}

function storeTreeNodeExpansionNSelectionSub(selectedNode) {
    try {
        sessionStorage.ExpansionSequence_BrandLevel = _expansionSequence;
        sessionStorage.SelectedTreeNodeId = selectedId;
    }
    catch (e) {
        alert(e);
    }
}

function handleTreeDataBoundSub(e) {
    //Do Nothing
}

function handleTreeNodeSelectionSub(selectedNode) {
    try {
        refreshTags();

        _networkObjectType = treeViewCtrl.dataItem(selectedNode).ItemType;

        $('#tvSelNameTags').html('Tags under <b>' + selectedName + '</b>');
        setStatus($('#operationStatus'));
    }
    catch (e) {
        alert(e);
    }
}

function saveTag(v_tagId, v_tagName, v_networkObjectId, v_isTagToMove) {
    $.ajax({
        url: '/tag/SaveTag',
        type: 'POST',
        dataType: 'json',
        data: { tagId: v_tagId, tagName: v_tagName, networkObjectId: v_networkObjectId, isTagToMove: v_isTagToMove, tagKey: _tagKey },
        success: function (data, textStatus, jqXHR) {
            refreshTags();
            statusMessage(true, data.lastActionResult);
            return true;
        },
        error: function (xhr, ajaxOptions, thrownError) {
            statusMessage(false, thrownError);
            return false;
        }
    });
}

$(".k-grid-add :first").click(function () {
    if (selectedId == undefined) {
        return false;
    }
    else {
        _IsNewTagToAdd = true;
    }
});

function recheckForUniquenessOfTagName(enteredTagName, networkObjectid) {
    $.ajax({
        url: '/tag/CheckTagAtBrandLevel',
        cache: false,
        type: 'GET',
        data: { 'tagName': enteredTagName, 'tagKey': _tagKey },
        success: function (data, textStatus, jqXHR) {
            if (data.IsNotUnique) {
                confirmWindow("Delete confirm", "Tag Name must be unique. A tag with this name already exists at Brand:'" + data.AtNetworkObjectName + "' level. Would like to move that tag over?", function () { }, "400px", "Ok", "Cancel", function (confirmation) {
                    if (confirmation === true) {
                        _isRecheckForUniquenessCompleted = true;
                        //Update the networkObjectId of this tag                                     
                        _isRecheckForUniquenessResult = saveTag(data.ExistingTagId, enteredTagName, networkObjectid, true);
                    }
                    else {
                        _isRecheckForUniquenessCompleted = true;
                        _isRecheckForUniquenessResult = false;
                    }
                });
            }
            else {
                _isRecheckProcessCompleted = true;
                _isRecheckForUniquenessResult = true;
            }
        },
        error: function (xhr) {
            _isRecheckForUniquenessCompleted = true;
            if (xhr.status == 401) {
                setMainStatus(false, xhr.statusText);
            }
            else {
                setMainStatus(false, "Internal server error");
            }
            _isRecheckForUniquenessResult = false;
        }
    });
}

//Resize Kendo TreeView And/Or Kendo Grid
$(window).load(function () { triggerResizeTree(); });
$(window).resize(function () { triggerResize(); });