var _tree = null;
var IsTreeNodeExpanded = false;
var parentNodeChecked = false;

var selectedMenuId = 0;
var selectedNetworkIds = new Array();
var mappingHub;
_initialExpansionItemType = NetworkObjectType.Root; //This is the @@Model.ItemType

var _networkObjectIdWithTypes = new Array();
var _networkObjectIds = new Array();

$(document).ready(function () {
    highlightMenubar("site", "siteactions");
    _networkObjectIds = new Array();
    $('#divAMContent').hide();

    // Declare a proxy to reference the hub. 
    //mappingHub = $.connection.pOSMapHub;

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
                parentId: "parentId",
                ItemType: "ItemType"
            }
        }
    });

    _tree = $("#treeView").kendoTreeView({
        checkboxes: {
            checkChildren: true,
            template: "<input type='checkbox' name='checkedNodes' value='#=item.id#' data-netType='#=item.ItemType#' data-name='#=item.Name#' />"
        },
        check: onSiteActionTreeCheck,
        dataSource: _root,
        dataTextField: "Name",
        dataImageUrlField: "Image",
        loadOnDemand: true,
        dataBound: function (e) {
            handleTreeDataBound(e);
        },
        expand: function (e) {
            var checkBoxes = $(e.node).find(":checkbox");
            if (checkBoxes != undefined) {
                var checkBoxesCnt = checkBoxes.length;
                if (checkBoxesCnt > 0) {
                    parentNodeChecked = checkBoxes[0].checked;
                }
            }
            IsTreeNodeExpanded = true;
        }
    });

    treeViewCtrl = $("#treeView").data("kendoTreeView");    

    
    // Create a function that the hub can call to broadcast messages.
    //mappingHub.client.updateProgress = function (percent, message, status, entity) {

    //    // enable button
    //    if (percent < 0 || percent == 100) {
    //        setConfirmUnload(false);
    //        $('#AutoMap, #AutoMap').removeAttr("disabled");

    //        //Enable Tabs
    //        //$(".tabElement").removeClass('disabledAnchor');
    //    }

    //    var rowStyle = "";
    //    if (status != null && status != undefined) {
    //        if (status == "Info") {
    //            rowStyle = "alert alert-info";
    //        }
    //        else if (status == "Success") {
    //            rowStyle = "alert alert-success";
    //        }
    //        else if (status == "Error") {
    //            rowStyle = "alert alert-danger";
    //        }
    //    }
    //    if (entity == null || entity == undefined) {
    //        entity = "";
    //    }
    //    row = $("<tr class='" + rowStyle + "'></tr>");
    //    col1 = $("<td>" + entity + "</td>");
    //    col2 = $("<td>" + message + "</td>");
    //    row.append(col1, col2).prependTo("#tb_mapProgress");

    //    $("#mapProgressBar").css('width', percent + '%');
    //    $("#mapProgressBar").html(percent + '%');
    //    triggerResize();
    //};

    //// Start the connection.
    //$.connection.hub.start().done(function () {

    //    $('#AutoMap').click(function () {
    //        //var networkObjsChecked = _tree.find(":checkbox:checked")
    //        //var networkObjsCheckedCnt = networkObjsChecked.length;

    //        if (selectedMenuId == 0 && _networkObjectIds.length == 0) {
    //            statusMessage("Info", "Select a menu and one or more network objects to map");
    //            return;
    //        }
    //        else if (selectedMenuId == 0) {
    //            statusMessage("Info", "Select a menu to map");
    //            return;
    //        }
    //        else if (_networkObjectIds.length == 0) {
    //            statusMessage("Info", "Select one or more network objects to map");
    //            return;
    //        }

    //        $('#divAMContent').show();
    //        //var networkObjectIds = new Array();
    //        //networkObjsChecked.each(function () {
    //        //    networkObjectIds.push(this.value);
    //        //});

    //        setConfirmUnload(true);
    //        $('#AutoMap, #AutoMap').attr("disabled", "disabled");

    //        // Call the MapMultipleSites method on the hub. 
    //        mappingHub.server.mapMultipleSites(selectedMenuId, _networkObjectIds);

    //        $('#tb_mapProgress tbody tr').remove();
    //        $("#mapProgressBar").css('width', '0%');

    //        //Disable Tab click
    //        //$(".tabElement").addClass('disabledAnchor');

    //    });
    //});
    // open a tab initially
    //$('#tabItemGroup a:first').tab('show');
    //adminTabSelected('#1A');


    reloadMenuSyncTab(_networkObjectIds);
});

function handleTreeDataBound(e) {
    if (IsTreeNodeExpanded) {
        $(e.node).find(":checkbox").attr("checked", parentNodeChecked);
        IsTreeNodeExpanded = false;
    }
}

function adminTabSelected(tabId) {
    $(".resizable-grid").removeClass("active").addClass('inactive');
    $(tabId).find(".resizable-grid").removeClass("inactive").addClass('active');

    if (tabId == '#1A') {

        reloadMenuSyncTab(_networkObjectIds);
    }
    else if (tabId == '#1B') {

        reloadAutoMapTab(_networkObjectIds);
    }
}

// show checked node IDs on datasource change
function onSiteActionTreeCheck(e) {
    var checkedNodes = [],
        message;

    var networkObjsChecked = _tree.find(":checkbox:checked")
    var networkObjsCheckedCnt = networkObjsChecked.length;

    //Initial load
    if (networkObjsCheckedCnt == 0 && _networkObjectIds.length == 0) {

        return;
    }

    if (_networkObjectIds.length != 0) {
        _networkObjectIds = new Array();
    }
    if (_networkObjectIdWithTypes.length != 0) {
        _networkObjectIdWithTypes = new Array();
    }
    
    networkObjsChecked.each(function () {
        _networkObjectIds.push(this.value);

        var typ = $(this).attr('data-netType');
        var name = $(this).attr('data-name');
        var netSelected = {
            id: this.value,
            typ: typ,
            name: name
        };
        _networkObjectIdWithTypes.push(netSelected);
    });

    //refreshTabs(_networkObjectIds);

    reloadMenuSyncTab(_networkObjectIds);
}
function refreshTabs(networkObjIds) {
    var activeTabId = $('#tabItemGroup .active a').attr('href')
    
    if (activeTabId == '#1A') {
        reloadMenuSyncTab(networkObjIds);
    }
    else if (activeTabId == '#1B') {
        reloadAutoMapTab(networkObjIds);
    }
}
function initMenuSyncTab(networkObjIds) {
    // target grid
    $("#gridSyncTarget").kendoGrid({
        dataSource: {
            type: "json",
            transport: {
                read: {
                    url: "/menusync/GetTargetList",
                    dataType: "json",
                    type: "GET",
                    contentType: "application/json; charset=utf-8",
                    cache: false,
                    data: { netIdsString: JSON.stringify(networkObjIds) }
                }
            },
            pageSize: __gridDefaultPageSize,
            requestEnd: function (e) {
                if (e.type != "read") {
                    refreshTargets();
                }
            }
        },
        dataBound: function (e) {
            scrollToTop(this);
            this.thead.find(":checkbox")[0].checked = false;
            triggerResizeGrid();
        },
        sortable: true,
        filterable: {
            operators: {
                string: { contains: "Contains", doesnotcontain: "Does Not Contain", eq: "Is Equal To", neq: "Is Not Equal To", startswith: "Starts With", endswith: "Ends With" }
            }
        },
        pageable: {
            refresh: true,
            pageSizes: __gridPageSizes
        },
        columns: [
            { field: "MenuSyncTargetId", title: "Select", width: 40, template: "<input type='checkbox' value=#=MenuSyncTargetId# onclick='onCheckboxClick(this)' />", headerTemplate: "<input type='checkbox' title='Select/Unselect All' onclick='onCheckUncheckAllClick(this)' />", sortable: false, filterable: false },
            { field: "TargetName", width: 160, title: "Target Name" },
            { field: "LastSyncDate", width: 160, title: "Last Sync Date", type: 'date', format: "{0:MM/dd/yyyy hh:mm tt}" },
            { field: "LastSyncStatus", width: 160, title: "Last Sync Status" }
        ]
    }).data("kendoGrid");

    // target detail grid
    $("#gridSyncTargetDetail").kendoGrid({
        dataSource: {
            type: "json",
            transport: {
                read: {
                    url: "/menusync/GetTargetDetailList",
                    dataType: "json",
                    type: "GET",
                    contentType: "application/json; charset=utf-8",
                    cache: false,
                    data: { netIdsString: JSON.stringify(networkObjIds) }
                }
            },
            schema: {
                data: "data", // records are returned in the "data" field of the response
                total: "total" // total number of records is in the "total" field of the response
            },
            serverSorting: true,
            sort: { field: "RequestedTime", dir: "desc" },
            serverFiltering: true,
            serverPaging: true,
            pageSize: __gridDefaultPageSize,
            requestStart: function (e) {
                if (e.type == "read") {
                    var selectedNetId = e.sender.transport.options.read.data.netIdsString;
                    if (selectedNetId == "" || selectedNetId == 0 || selectedNetId == null) {
                        e.preventDefault();
                    }
                }
            }
        },
        dataBound: function (e) {
            scrollToTop(this);
            triggerResizeGrid();
        },
        sortable: true,
        filterable: {
            operators: {
                string: { contains: "Contains", doesnotcontain: "Does Not Contain", eq: "Is Equal To", neq: "Is Not Equal To", startswith: "Starts With", endswith: "Ends With" }
            }
        },
        pageable: {
            refresh: true,
            pageSizes: __gridPageSizes
        },
        columns: [
            { field: "TargetName", width: 160, title: "Target Name" },
            { field: "NetworkObject.Name", width: 160, title: "Site Name" },
            { field: "MenuName", width: 160, title: "Menu Name" },
            { field: "SyncStatus", width: 160, title: "Status" },
            { field: "SyncMessage", width: 160, title: "Sync Message" },
            { field: "RequestedTime", width: 160, title: "Requested Time", type: 'date', format: "{0:MM/dd/yyyy hh:mm tt}" },
            { field: "ResponseTime", width: 160, title: "Response Time", type: 'date', format: "{0:MM/dd/yyyy hh:mm tt}" },
        ]
    }).data("kendoGrid");

    // sync target
    $("#btnMenuSync").click(function () {
        var grid = $("#gridSyncTarget").data("kendoGrid");

        var targetsChecked = grid.tbody.find(":checkbox:checked");
        var targetsCheckedCnt = targetsChecked.length;

        //var networkObjsChecked = _tree.find(":checkbox:checked")
        //var networkObjsCheckedCnt = networkObjsChecked.length;

        if (targetsCheckedCnt == 0 && _networkObjectIds.length == 0) {
            statusMessage("Info", "Select one or more targets and network objects to sync");
            return;
        }
        else if (targetsCheckedCnt == 0) {
            statusMessage("Info", "Select one or more targets to sync");
            return;
        }
        else if (_networkObjectIds.length == 0) {
            statusMessage("Info", "Select one or more network objects to sync");
            return;
        }

        var targetList = new Array();
        targetsChecked.each(function () {
            targetList.push(grid.dataItem($(this).closest("tr")));
        });

        ////use _networkObjectIds
        //var networkObjectIds = new Array();
        //networkObjsChecked.each(function () {
        //    networkObjectIds.push(this.value);
        //});

        //Sync Targets
        $.ajax({
            url: '/menusync/Sync',
            type: 'POST',
            dataType: 'json',
            data: { networkObjectIds: JSON.stringify(_networkObjectIds), targets: JSON.stringify(targetList) },
            beforeSend: function (e) {
                $("#btnMenuSync").button('loading');
            },
            success: function (data, textStatus, jqXHR) {
                $("#btnMenuSync").button('reset');
                refreshTargets();
                statusMessage(data.status, data.lastActionResult);
            },
            error: function (xhr, ajaxOptions, thrownError) {
                statusMessage(false, thrownError);
            }
        });
    });


    $("#btnDeleteSyncHistroy").click(function () {

        if (_networkObjectIdWithTypes.length == 0) {
            statusMessage("Info", "Select one or more network objects to delete History");
            return;
        }

        confirmWindow("Delete confirm", "Are you sure to delete the Sync History?", function () { }, "400px", "OK", "Cancel", function (data) {
            if (data === true) {
                //Delete Sync History
                $.ajax({
                    url: '/menusync/DeleteSyncHistory',
                    type: 'POST',
                    dataType: 'json',
                    data: { networkObjectDetails: JSON.stringify(_networkObjectIdWithTypes) },
                    beforeSend: function (e) {
                        $("#btnDeleteSyncHistroy").button('loading');
                    },
                    success: function (data, textStatus, jqXHR) {
                        $("#btnDeleteSyncHistroy").button('reset');
                        refreshTargetHistory();
                        statusMessage(data.status, data.lastActionResult);
                    },
                    error: function (xhr, ajaxOptions, thrownError) {
                        statusMessage(false, thrownError);
                    }
                });
            }
        });
    });

    $("#btnClearTreeViewSelection").click(function () {
        var rootLevelCheckBox = _tree.find(":checkbox :first");
        var checkedCount = _tree.find(":checked");
        if (checkedCount.length > 0) {
            rootLevelCheckBox.click();
        }
        return false;
    });
}

//refresh Targets
function refreshTargets() {
    var grid = $("#gridSyncTarget").data("kendoGrid").dataSource;
    grid.read();

    refreshTargetHistory();
}

//refresh Target History
function refreshTargetHistory() {

    var gridDetails = $("#gridSyncTargetDetail").data("kendoGrid").dataSource;
    gridDetails.read();
}

function reloadMenuSyncTab(netIds) {

    var gridMenuSync = $("#gridSyncTarget").data("kendoGrid")
    var gridMenuSyncDetail = $("#gridSyncTargetDetail").data("kendoGrid")
    if (gridMenuSync == undefined || gridMenuSyncDetail == undefined)
    {        
        initMenuSyncTab(_networkObjectIds);
    }
    else
    {
        //Reload Menus
        var gdMenuSyncDS = gridMenuSync.dataSource;
        gdMenuSyncDS.transport.options.read.data.netIdsString = JSON.stringify(netIds);
        gdMenuSyncDS.read();

        //Reload History
        var gdMenuSyncDetailDS = gridMenuSyncDetail.dataSource;
        gdMenuSyncDetailDS.transport.options.read.data.netIdsString = JSON.stringify(netIds);
        gdMenuSyncDetailDS.read();
    }
}

//function initAutoMapTab(netIds) {

//    $("#menuSelection").kendoComboBox({
//        placeholder: "Please Select a Menu...",
//        dataTextField: "Name",
//        dataValueField: "MenuId",
//        dataSource: {
//            type: "json",
//            transport: {
//                read: {
//                    url: "/Menu/GetALLMenuList",
//                    cache: false,
//                    data: { 'netIdsString': JSON.stringify(netIds), 'breakcache': new Date().getTime() },
//                    dataType: "json",
//                    type: "GET",
//                    contentType: "application/json; charset=utf-8"
//                }
//            }
//        },
//        select: function (e) {
//            var dataItem = this.dataItem(e.item.index());
//            selectedMenuId = dataItem.MenuId;
//        }
//    });

//}

//function reloadAutoMapTab(netIds) {
//    //Reload Menus
//    var ddlMenuDS = $("#menuSelection").data("kendoComboBox").dataSource;
//    ddlMenuDS.transport.options.read.data.netIdsString = JSON.stringify(netIds);
//    ddlMenuDS.read();

//    //reset DLL
//    var ddlMenu = $("#menuSelection").data("kendoComboBox");
//    ddlMenu.value("");
//}
//function setConfirmUnload(on) {
//    window.onbeforeunload = (on) ? unloadMessage : null;
//}

//function unloadMessage() {
//    return 'Auto-mapping is still in progress.';
//}
//Resize Kendo TreeView And/Or Kendo Grid
$(window).load(function () { triggerResizeTree(); });
$(window).resize(function () { triggerResize(); });