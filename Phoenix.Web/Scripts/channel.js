var _tree = null;
var _networkObjectType = null;
var _tagKey = TagKeys.Channel;
var _isValidTreeNodeSelected = false;

var valueDelimiter = "^`";
_expansionSequence = sessionStorage.ExpansionSequence_BrandLevel != undefined && sessionStorage.ExpansionSequence_BrandLevel != "" ? sessionStorage.ExpansionSequence_BrandLevel.split(',') : new Array();
_initialExpansionAfterSelectionItemType = _initialExpansionItemType = NetworkObjectType.Root; //This is the @@Model.ItemType

$(document).ready(function () {
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

           

//channel delete
$("#btnChannelDelete").click(function () {
    setStatus($('#operationStatus'));

    var grid = $("#gridChannel").data("kendoGrid");

    var tagsChecked = grid.tbody.find(":checkbox:checked");
    var tagsCheckedCnt = tagsChecked.length;

    if (tagsCheckedCnt == 0) {
        statusMessage("Info", "Select one or more channels to delete");
        return;
    }

    var tagList = new Array();
    tagsChecked.each(function () {
        tagList.push(grid.dataItem($(this).closest("tr")));
    });
    confirmWindow("Delete confirm", "Are you sure you want to delete the selected channel(s)?", function () { }, "400px", "OK", "Cancel", function (data) {
        if (data === true) {
            deleteTags(tagList);
        }
    });
});
        
function chAdditionalReadData(e) {
    e.networkObjectId = selectedId,
    e.tagKey = _tagKey
    return e;
}
function chAdditionalSaveData(e) {
    //e.NetworkObjectId = selectedId,
    //e.tagId= e.TagId, 
    //e.tagName = e.TagName,
    e.NetworkObjectId = selectedId,
    //e.isTagToMove = false,
    e.TagKey = _tagKey
    return e;
}
function chError(e) {
    if (e.errors) {
        grid = $("#gridChannel").data("kendoGrid");
        grid.one("dataBinding", function (e) {
            e.preventDefault();   // cancel grid rebind if error occurs                             
        });
        for (var error in e.errors) {
            kendoGridShowMessage(grid.editable.element, error, e.errors[error].errors);
        }
    }
}
        
function chRequestStart(e) {
    if (e.type === "read") {
        var selectedNetId = e.sender.transport.options.read.data.networkObjectId;
        if (selectedNetId == "" || selectedNetId == 0 || selectedNetId == null) {
            e.preventDefault();
        }
    }
}
function chRequestEnd(e) {
    if (e.type != "read") {
        //Check is the response contains Errors
        if (e.response != undefined && e.response.Errors != undefined) {
        }
        else {
            if (e.type === "update") {
                var name = e.response.TagName;
                if (name == undefined) {
                    name = "";
                }
                statusMessage(true, "Updated the Channnel " + name);
            }
            else if (e.type === "create") {
                var name = e.response.TagName;
                if (name == undefined) {
                    name = "";
                }
                statusMessage(true, "Created the Channnel " + name);
            }
            var grid = $("#gridChannel").data("kendoGrid").dataSource;
            grid.read();
        }
    }
}

function chEdit(e) {
    $("input[name='TagName'] :first").focus();
}
function chSave(e) {
    //e.preventDefault();
    //if (e.model.isNew()) {
    //    e.model.TagId = 0;
    //    e.model.NetworkObjectId = selectedId;
    //}
    //saveTag(e.model.TagId, e.model.TagName, e.model.NetworkObjectId, false);
}

//refresh Tags
function refreshTags() {
    var grid = $("#gridChannel").data("kendoGrid").dataSource;
    grid.transport.options.read.data.networkObjectId = selectedId;
    grid.page(1);
}

function onChannelGridDataBound(e) {
    var grid = $("#gridChannel").data("kendoGrid");
    if (grid != undefined) {

        if (!_isValidTreeNodeSelected) {
            var createButton = grid.wrapper.find(".k-grid-add");
            createButton.addClass('disabledAnchor').addClass("k-state-disabled").removeClass("k-grid-add");
        }
        else {
            var createButton = grid.wrapper.find(".k-state-disabled");
            createButton.removeClass('disabledAnchor').removeClass("k-state-disabled").addClass("k-grid-add");
        }


        var taglist = grid.dataSource._data;
        var tagListCount = taglist.length;
        var taglistString = "";
        if (tagListCount > 0) {

            ////1. store Complete Tag List
            //for (var loopCntr = 0; loopCntr < tagListCount; loopCntr++) {
            //    taglistString += valueDelimiter + taglist[loopCntr].TagId + valueDelimiter + taglist[loopCntr].TagName.toUpperCase();
            //}
            ////1.1
            //sessionStorage.GridData = taglistString + valueDelimiter;

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
    grid.tbody.find("tr .k-grid-Delete").each(function () {
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
function deleteChannel(e) {
    e.preventDefault();
    var dataItem = this.dataItem($(e.currentTarget).closest("tr"));

    var confirmationMsg = "Are you sure you want to delete the channel: '" + dataItem.TagName + "' ?";

    if (dataItem.HasAssociatedData) {
        confirmationMsg = "This channel: '" + dataItem.TagName + "' has got some associated data.";
        confirmationMsg += "<br/> Are you sure you want to delete this channel?";
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

function chDetailInit(e) {
    if (e.data.TagId == null)
        return;

    var detailRow = e.detailRow;
    $.ajax({
        url: '/tag/GetTagAssociatedData',
        type: 'GET',
        dataType: 'json',
        data: { tagId: e.data.TagId, networkObjectId: selectedId, tagKey: _tagKey },
        success: function (data, textStatus, jqXHR) {
            if (data.status) {
                var liAssetsCtrl = $(detailRow).find("#liMenus");
                var liItemsCtrl = $(detailRow).find("#liTargets");

                liAssetsCtrl.html(liAssetsCtrl.html() + data.associatedData["menus"]);
                liItemsCtrl.html(liItemsCtrl.html() + data.associatedData["targets"]);
            }
            else
            {
                statusMessage(data.status, data.msg);
            }
        },
        error: function (xhr, ajaxOptions, thrownError) {
            statusMessage(false, thrownError);
        }
    });
}

function restoreSelectionSub() {
    selectedId = sessionStorage.SelectedTreeNodeId;
}

function storeTreeNodeExpansionNSelectionSub(selectedNode) {
    sessionStorage.ExpansionSequence_BrandLevel = _expansionSequence;
    sessionStorage.SelectedTreeNodeId = selectedId;
}

function handleTreeDataBoundSub(e) {
    //Do Nothing
}

function handleTreeNodeSelectionSub(selectedNode) {
    refreshTags();

    _networkObjectType = treeViewCtrl.dataItem(selectedNode).ItemType;
    _isValidTreeNodeSelected = false;
    if (_networkObjectType != undefined && _networkObjectType == NetworkObjectType.Brand) {
        //Enable 'Add New Channel' button
        $(".k-grid-add :first").removeClass('disabledAnchor').removeClass("k-state-disabled");
        $(".k-grid-add :first").removeAttr('disabled', 'disabled');
        _isValidTreeNodeSelected = true;
    }

    $('#tvSelNameChannels').html('Channels under <b>' + selectedName + '</b>');
    setStatus($('#operationStatus'));
}

//Resize Kendo TreeView And/Or Kendo Grid
$(window).load(function () { triggerResizeTree(); });
$(window).resize(function () { triggerResize(); });