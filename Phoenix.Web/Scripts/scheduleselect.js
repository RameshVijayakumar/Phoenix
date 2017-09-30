_expansionSequence = sessionStorage.ExpansionSequence_Schedule != undefined && sessionStorage.ExpansionSequence_Schedule != "" ? sessionStorage.ExpansionSequence_Schedule.split(',') : new Array();
_initialExpansionAfterSelectionItemType = _initialExpansionItemType = highestNetworkLevelAccess != undefined && highestNetworkLevelAccess != "" ? parseInt(highestNetworkLevelAccess) : NetworkObjectType.Root; //This is the @@Model.ItemType

var selectedId = 0;
// Schedule edit
function showSchDetails(e) {
    e.preventDefault();

    var dataItem = this.dataItem($(e.currentTarget).closest("tr"));

    document.location = '/Schedule/ScheduleEdit/' + dataItem.ScheduleId + '?netId=' + selectedId;

}

// Schedule delete
function deleteSch(e) {
    e.preventDefault();

    var grid = this;
    var row = $(e.currentTarget).closest("tr");
    var dataItem = grid.dataItem(row);
    if (row != null && row != undefined) {
        confirmWindow("Delete confirm", "Are you sure you want to delete " + dataItem.SchName + " Schedule?", function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {
                $.ajax({
                    url: '/Schedule/DeleteSchedule',
                    type: 'GET',
                    dataType: 'json',
                    data: { id: dataItem.ScheduleId, netId: selectedId },
                    success: function (data, textStatus, jqXHR) {

                        statusMessage(data.status, data.lastActionResult);

                        if (data.lastActionResult.indexOf("failed") != -1) return;

                        var grdsch = $("#gridSchedule").data("kendoGrid").dataSource;
                        grdsch.transport.options.read.data.netId = selectedId;
                        grdsch.page(1);
                        grdsch.read();
                    },
                    error: function (xhr) {

                    }
                });
            }
            else {

            }
        });
    }
}

// Schedule dsiable
function disableSch(e) {
    e.preventDefault();

    var grid = this;
    var row = $(e.currentTarget).closest("tr");
    var dataItem = grid.dataItem(row);
    if (row != null && row != undefined) {
        confirmWindow("Disable confirm", "Are you sure you want to disable " + dataItem.SchName + " Schedule?", function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {
                $.ajax({
                    url: '/Schedule/DisableSchedule',
                    type: 'GET',
                    dataType: 'json',
                    data: { id: dataItem.ScheduleId, netId: selectedId },
                    success: function (data, textStatus, jqXHR) {

                        statusMessage(data.status, data.lastActionResult);

                        if (data.lastActionResult.indexOf("failed") != -1) return;

                        var grdsch = $("#gridSchedule").data("kendoGrid").dataSource;
                        grdsch.transport.options.read.data.netId = selectedId;
                        grdsch.page(1);
                        grdsch.read();
                    },
                    error: function (xhr) {

                    }
                });
            }
            else {

            }
        });
    }
}
function enableSch(e) {
    e.preventDefault();

    var grid = this;
    var row = $(e.currentTarget).closest("tr");
    var dataItem = grid.dataItem(row);
    if (row != null && row != undefined && dataItem.IsActive == false) {
        confirmWindow("Enable confirm", "Are you sure you want to enable " + dataItem.SchName + " Schedule?", function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {
                $.ajax({
                    url: '/Schedule/EnableSchedule',
                    type: 'GET',
                    dataType: 'json',
                    data: { id: dataItem.ScheduleId, netId: selectedId },
                    success: function (data, textStatus, jqXHR) {
                        statusMessage(data.status, data.lastActionResult);

                        if (data.lastActionResult.indexOf("failed") != -1) return;
                        var grid = $("#gridSchedule").data("kendoGrid").dataSource
                        grid.read();
                    },
                    error: function (xhr) {

                    }
                });
            }
        });
    }
}
function copySch(e) {
    e.preventDefault();

    var dataItem = this.dataItem($(e.currentTarget).closest("tr"));
    confirmWindow("Copy confirm", "Are you sure you want to copy " + dataItem.SchName + " Schedule?", function () { }, "400px", "Ok", "Cancel", function (data) {
        if (data === true) {
            $.ajax({
                url: '/Schedule/CopySchedule',
                type: 'GET',
                dataType: 'json',
                data: { 'model': JSON.stringify(dataItem), 'netId': selectedId },
                success: function (data, textStatus, jqXHR) {

                    statusMessage(data.status, data.lastActionResult);

                    if (data.lastActionResult.indexOf("failed") != -1) return;

                    var grdsch = $("#gridSchedule").data("kendoGrid").dataSource;
                    grdsch.transport.options.read.data.netId = selectedId;
                    grdsch.read();

                },
                error: function (xhr) {

                }
            });
        }
        else {

        }
    });
}
function revertSch(e) {
    e.preventDefault();

    var dataItem = this.dataItem($(e.currentTarget).closest("tr"));
    confirmWindow("Revert confirm", "Are you sure you want to revert " + dataItem.SchName + " Schedule changes?", function () { }, "400px", "Ok", "Cancel", function (data) {
        if (data === true) {
            $.ajax({
                url: '/Schedule/RevertSchedule',
                type: 'GET',
                dataType: 'json',
                data: { id: dataItem.ScheduleId, netId: selectedId },
                success: function (data, textStatus, jqXHR) {

                    statusMessage(data.status, data.lastActionResult);

                    if (data.lastActionResult.indexOf("failed") != -1) return;

                    var grdsch = $("#gridSchedule").data("kendoGrid").dataSource;
                    grdsch.transport.options.read.data.netId = selectedId;
                    grdsch.read();

                },
                error: function (xhr) {

                }
            });
        }
        else {

        }
    });
}
$("#btnCreateSch").bind("click", function () {
    document.location = '/Schedule/ScheduleEdit?id=0&netId=' + selectedId;
});

function onGridSchDataBound(e) {
    var grid = e.sender;
    if (grid != undefined) {
        dataView = this.dataSource.view();
        if (dataView.length > 0) {
            for (var i = 0; i < dataView.length; i++) {

                if (dataView[i].IsActive === false) {
                    var uid = dataView[i].uid;
                    $("#gridSchedule tbody").find("tr[data-uid=" + uid + "]").addClass("disableRowClass");
                }
            }
        }
    }
    //Disable Create button until validate Network is selected

    if (selectedNodeType < 1) {
        var createButton = grid.wrapper.find(" .k-grid-add");
        createButton.addClass('disabledAnchor').addClass("k-state-disabled");
    }
    else {
        var createButton = grid.wrapper.find(" .k-state-add");
        createButton.removeClass('disabledAnchor').removeClass("k-state-disabled").addClass("k-grid-add");
    }

    //Selects all delete buttons
    grid.tbody.find("tr .k-grid-delete").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        ////Check in the current dataItem if the row is editable
        //if (currentDataItem.IsOverride === true) {
        //    //$(this).html("Disable");
        //    $(this).remove();
        //}
        //else if (currentDataItem.IsCreatedAtThisNetwork === false) {
        //    //$(this).html("Disable");

        //    $(this).remove();
        //}

        //if (currentDataItem.IsActive === false) {
        //    $(this).remove();
        //}

        if (currentDataItem.IsActive === false || currentDataItem.IsCreatedAtThisNetwork === false || currentDataItem.IsOverride === true) {
            $(this).addClass('disabledAnchor').addClass("k-state-disabled");
        }
        else {
            $(this).removeClass('disabledAnchor').removeClass("k-state-disabled");
        }

    })

    //Selects all disable buttons
    grid.tbody.find("tr .k-grid-disable").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        if (currentDataItem.IsActive === false) {
            $(this).remove();
        }

    })
    //Selects all enable buttons
    grid.tbody.find("tr .k-grid-customEnable").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        //Check in the current dataItem if the row is active
        if (currentDataItem.IsActive === true) {
            $(this).remove();
        }

    })
    //Selects all enable buttons
    grid.tbody.find("tr .k-grid-edit").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        //Check in the current dataItem if the row is active
        if (currentDataItem.IsActive === false) {
            $(this).addClass('disabledAnchor').addClass("k-state-disabled");
        }
        else {
            $(this).removeClass('disabledAnchor').removeClass("k-state-disabled");
        }

    })
    //Selects all enable buttons
    grid.tbody.find("tr .k-grid-copy").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        //Check in the current dataItem if the row is active
        if (currentDataItem.IsActive === false) {
            $(this).addClass('disabledAnchor').addClass("k-state-disabled");
        }
        else {
            $(this).removeClass('disabledAnchor').removeClass("k-state-disabled");
        }

    })
    //Selects all revert buttons
    grid.tbody.find("tr .k-grid-revert").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        //Check in the current dataItem if the row is active
        if (currentDataItem.IsOverride === false) {
            $(this).remove();
        }

        if (currentDataItem.IsActive === false) {
            $(this).addClass('disabledAnchor').addClass("k-state-disabled");
        }
        else {
            $(this).removeClass('disabledAnchor').removeClass("k-state-disabled");
        }

    })
    triggerResizeGrid();
}
$(function () {
    highlightMenubar("schedule");
    $("#btnCreateSch").hide();
var _root = new kendo.data.HierarchicalDataSource({
    transport: {
        read: {
            cache: false,
            url: "/site/networkObjectTreeView",
            dataType: "json",
            data: { 'breakcache': new Date().getTime(), 'includeaccess': true, 'networkObjectType': _initialExpansionItemType }
        }
    },
    schema: {
        model: {
            id: "Id",
            hasChildren: "HasChildren",
            expanded: "expanded",
            parentId: "parentId"
        }
    }
});

$("#treeview").kendoTreeView({
    dataSource: _root,
    dataImageUrlField: "Image",
    dataTextField: "Name",
    loadOnDemand: true, //do NOT recurse with databound to expand nodes
    dataBound: function (e) {
        handleTreeDataBound(e);
    },
    select: function (e) {
        handleTreeNodeSelection(e.node);
    }
});

treeViewCtrl = $("#treeview").data("kendoTreeView");
});

function restoreSelectionSub() {
    try {
        selectedId = sessionStorage.SelectedTreeNodeId_Schedule;
    }
    catch (e) {
        alert(e);
    }
}

function storeTreeNodeExpansionNSelectionSub(selectedNode) {
    try {
        sessionStorage.ExpansionSequence_Schedule = _expansionSequence;
        sessionStorage.SelectedTreeNodeId_Schedule = selectedId;
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
        //new node selected clear previous messages
        selectedId = treeViewCtrl.dataItem(selectedNode).id;
        selectedNodeType = treeViewCtrl.dataItem(selectedNode).ItemType;
        if (selectedNodeType != 1) {
            $("#btnCreateSch").show();
        }
        else {
            $("#btnCreateSch").hide();
        }
        

        var grdsch = $("#gridSchedule").data("kendoGrid").dataSource;
        grdsch.transport.options.read.data.netId = selectedId;
        grdsch.page(1);
        grdsch.read();
    }
    catch (e) {
        alert(e);
    }
}
function onScheduleSortChange(e) {

    var grid = $("#gridSchedule").data("kendoGrid");
    var skip = grid.dataSource.skip(),
                        oldIndex = e.oldIndex + skip,
                        newIndex = e.newIndex + skip,
                        data = grid.dataSource.data(),
                        dataItem = grid.dataSource.getByUid(e.item.data("uid"));

    grid.dataSource.remove(dataItem);
    grid.dataSource.insert(newIndex, dataItem);

    $.ajax({
        url: '/Schedule/MoveSchedule',
        type: 'POST',
        dataType: 'json',
        data: { 'model': JSON.stringify(dataItem), 'netId': selectedId, 'newSortOrder': newIndex, 'oldSortOrder': oldIndex },
        beforeSend: function (e) {
            //showWaitSpinner();
        },
        success: function (data, textStatus, jqXHR) {
            //hideWaitSpinner();
            statusMessage(data.status, data.lastActionResult);

            if (data.lastActionResult.indexOf("failed") != -1) return;
            var grid = $("#gridSchedule").data("kendoGrid").dataSource;
            grid.page(1);
            grid.read();
        },
        error: function (xhr) {

        }
    });

}
//Resize Kendo TreeView And/Or Kendo Grid
$(window).load(function () { triggerResizeTree(); });
$(window).resize(function () { triggerResize(); });
