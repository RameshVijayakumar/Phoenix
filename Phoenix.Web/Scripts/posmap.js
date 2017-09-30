var selectedMenuId = -1;
var _currNetworkObjectId = 0;
//As kendoTreeview.helper.js needs property named "selectedId"
var selectedId = _currNetworkObjectId;
var renderedItemGrids = new Array();
var _refreshedItemGrids = new Array();
 
_expansionSequence = sessionStorage.ExpansionSequence_tvNetObjects != undefined && sessionStorage.ExpansionSequence_tvNetObjects != "" ? sessionStorage.ExpansionSequence_tvNetObjects.split(',') : new Array();
_initialExpansionAfterSelectionItemType = _initialExpansionItemType = highestNetworkLevelAccess != undefined && highestNetworkLevelAccess != "" ? parseInt(highestNetworkLevelAccess) : NetworkObjectType.Brand; //This is the @@Model.ItemType
    
$(function () {                            

    highlightMenubar("menu");
    //$("#windowPOSDataMap").kendoWindow({
    //	width: "800px",
    //	height: "650px",
    //	title: "Operational POS Data",
    //	modal: true,
    //	animation: false,
    //	visible: false
    //});
    //$("#windowAutoMapBySite").kendoWindow({
    //	width: "600px",
    //	height: "610px",
    //	title: "Auto Map Using Reference Site",
    //	modal: true,
    //	animation: false,
    //	visible: false,
    //	close: function () {
    //	}
    //});



    // open a tab initially
    $('#tabItemGroup a:first').tab('show');

    var tvNetObjectsDataSource = new kendo.data.HierarchicalDataSource({
        transport: {
            read: {
                url: "/site/networkObjectTreeView",
                dataType: "json",
                data: { 'breakcache': new Date().getTime(), 'includeaccess': true, 'networkObjectType': _initialExpansionItemType, 'includefeatures': true, }
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

    function tvNetObjectsOnDataBound(e) {
        try
        {
            handleTreeDataBound(e);}
        catch (ex) {
            alert(ex);
        }
    }

    setupNetObjectTreeview($("#tvNetObjects"), $('#lblSelectedNetObjName'), $('#inpSelectedNetObjId'), tvNetOnSelect, tvNetObjectsOnDataBound, tvNetObjectsDataSource);

    treeViewCtrl = $("#tvNetObjects").data("kendoTreeView");    

    $('#btnSiteSelectOpen').attr("disabled", "disabled");
    $("#menuSelection").kendoComboBox({
        placeholder: "Please Select a Menu...",
        dataTextField: "InternalName",
        dataValueField: "MenuId",
        dataSource: {
            type: "json",
            transport: {
                read: {
                    url: "/Menu/GetMenuList",
                    data: { 'netId': $('#inpSelectedNetObjId').val(), 'networkType': 0, 'breakcache': new Date().getTime() },
                    dataType: "json",
                    type: "GET",
                    contentType: "application/json; charset=utf-8"
                }
            },
            requestStart: function (e) {
                if (e.type === "read") {
                    if (e.sender.transport.options.read.data.netId == "" || e.sender.transport.options.read.data.netId == 0) {
                        e.preventDefault();
                    }
                }
            },
        },
        select: function (e) {
            var dataItem = this.dataItem(e.item.index());
            selectedMenuId = dataItem.MenuId;
            setMainStatus();
            $('#btnSiteSelectOpen').removeAttr("disabled");


            if (_currNetworkObjectId != null && _currNetworkObjectId != 0) {

                if (sessionStorage.getItem('ni.type') != null && (parseInt(sessionStorage.getItem('ni.type')) == NetworkObjectType.Site)) {
                }
                // clear all item grids, but reload just current one
                clearAllGrids();
                reloadSelectedTabGrid();
            }
            //}
				
        }
    });
    $('#btnViewODS').attr("disabled", "disabled");

    setupNetObjectTreeview($("#tvAutoMapNetObjects"), $('#lblAutoMapSelNetObjName'), $('#inpAutoMapSelNetObjId'));

    //$("#btnPOSDataSelOK").bind("click", function () {

    //	var selItemId = $('#inpItemId').val();
    //	var selODSPOSId = $('#inpOPSPOSDataId').val();

    //	// save to database
    //	$.ajax({
    //		url: '/datamap/POSManualMap?networkObjectId=' + _currNetworkObjectId + '&itemId=' + selItemId + '&odsPOSId=' + selODSPOSId + '&menuId=' + selectedMenuId,
    //		type: 'POST',
    //		success: function (data, textStatus, jqXHR) {

    //			statusMessage(data.status, data.message);

    //			if (data.status == true) {
    //				reloadSelectedTabGrid();
    //			}

    //		},
    //		error: function (xhr) {
    //			statusMessage("Error", "Internal server error");
    //		},
    //		beforeSend: function () {
    //			statusMessage("Info", "Please wait...",null,null,null,false);
    //		},
    //		//complete: function () {
    //		//}
    //	});
    //	// update item grid with new PLU & statusDisplay

    //	$("#windowPOSDataMap").data("kendoWindow").close();
    //});

    //$("#btnPOSDataSelCancel").bind("click", function () {
    //	$("#windowPOSDataMap").data("kendoWindow").close();
    //});


    //$("#btnAutoMapPLU").bind("click", function () {

    //	if (!(_currNetworkObjectId && _currNetworkObjectId > 0)) {
    //		return;
    //	}
    //	if (selectedMenuId < 0) {
    //		setMainStatus(false, "Please select a Menu");
    //		return;
    //	}
    //	$.ajax({
    //		url: '/datamap/AutoMapPOSByPLU',
    //		type: 'POST',
    //		data: { 'networkObjectId': _currNetworkObjectId, "MenuId": selectedMenuId },
    //		success: function (data, textStatus, jqXHR) {

    //			statusMessage(data.status, data.message);

    //			if (data.status == true) {
    //				// clear all item grids, but reload just current one
    //				clearAllGrids();
    //				reloadSelectedTabGrid();
    //			}
    //		},
    //		error: function (xhr) {
    //			statusMessage("Error", "Internal server error");
    //		},
    //		beforeSend: function () {
    //			$('#btnAutoMapPLU').attr("disabled", "disabled");
    //			statusMessage("Info", "Performing auto map by PLU. Please wait...",null,null,null,false);
    //		},
    //		complete: function () {
    //			$('#btnAutoMapPLU').removeAttr("disabled");
    //		}
    //	});
    //});
		
    //$("#btnOpenAutoMapAllSites").bind("click", function () {

    //	if (_currNetworkObjectId == null || _currNetworkObjectId == 0) {
    //		return;
    //	}
    //	if (selectedMenuId < 0) {
    //		setMainStatus(false, "Please select a Menu");
    //		return;
    //	}
    //	$("#windowAutoMapAllSites").data("kendoWindow").center().open();
    //});
    //$("#btnOpenAutoMapBySite").bind("click", function () {

    //	if (_currNetworkObjectId == null || _currNetworkObjectId == 0) {
    //		return;
    //	}
    //	if (selectedMenuId < 0) {
    //		setMainStatus(false, "Please select a Menu");
    //		return;
    //	}
    //	setStatus($('#autoMapBySiteStatus'));
    //	$('#lblAutoMapNetObjName').text($('#inpSelectedSiteName').text());
    //	$("#windowAutoMapBySite").data("kendoWindow").center().open();
    //});

    //$('#btnAutoMapBySite').bind("click", function () {
			
    //	setStatus($('#autoMapBySiteStatus'));

    //	var refSiteId = $('#inpAutoMapSelNetObjId').val();
    //	if (refSiteId == _currNetworkObjectId) {
    //		return;
    //	}

    //	$.ajax({
    //		url: '/datamap/AutoMapPOSBySite', 
    //		type: 'POST',
    //		data: { 'networkObjectId': _currNetworkObjectId, 'refNetworkObjectId': refSiteId, "MenuId": selectedMenuId },
    //		success: function (data, textStatus, jqXHR) {

    //			setStatus($('#autoMapBySiteStatus'), data.status, data.message);

    //			if (data.status == true) {
    //				// clear all item grids, but reload just current one
    //				clearAllGrids();
    //				reloadSelectedTabGrid();
    //			}

    //		},
    //		error: function (xhr) {
    //			setStatus($('#autoMapBySiteStatus'), false, "Internal server error");
    //		},
    //		beforeSend: function () {
    //			$('#btnAutoMapBySite,#btnAutoMapBySiteClose').attr("disabled", "disabled");
    //			setStatus($('#autoMapBySiteStatus'), "info", "Performing auto map by site. Please wait...");
    //		},
    //		complete: function () {
    //			$('#btnAutoMapBySite,#btnAutoMapBySiteClose').removeAttr("disabled");
    //		}
    //	});
    //});

    //$('#btnAutoMapBySiteClose').bind("click", function () {
    //	$("#windowAutoMapBySite").data("kendoWindow").close();
    //});

    //// Declare a proxy to reference the hub. 
    //var mappingHub = $.connection.pOSMapHub;

    //// Create a function that the hub can call to broadcast messages.
    //mappingHub.client.updateProgress = function (percent, message) {

    //	// enable button
    //	if (percent < 0 || percent == 100) {
    //		setConfirmUnload(false);
    //		$('#AutoMap, #AutoMap').removeAttr("disabled");
    //	}
    //	$('#mapProgressMsg').html(message + '<br />' + $('#mapProgressMsg').html());
    //	$("#mapProgressBar").css('width', percent + '%');
    //	$("#mapProgressBar").html(percent + '%');

    //};


    //// Start the connection.
    //$.connection.hub.start().done(function () {

    //	$('#AutoMap').click(function () {

    //		setConfirmUnload(true);
    //		$('#AutoMap, #AutoMap').attr("disabled", "disabled");

    //		// Call the Send method on the hub. 
    //		mappingHub.server.mapAll(selectedMenuId);

    //		$('#mapProgressMsg').html('');
    //		$("#mapProgressBar").css('width', '0%');

    //	});
    //});

    //$('#AutoMapClear').click(function () {
    //	$('#mapProgressMsg').html('');
    //	$("#mapProgressBar").css('width', '0%');

    //});

});


function tvNetOnSelect(dataItem) {
    //try {
        if (dataItem.HasAccess != null) {
            if (dataItem.HasAccess == false) {
                $('#infoMessage').text("Error: No Access Permissions to " + NetworkTypesToString(dataItem.ItemType));
                $('#infoMessage').show();
                selNameElement.text("");
                selIdElement.val("");
                sessionStorage.setItem('ni.name', "");
                sessionStorage.setItem('ni.type', 0);
                sessionStorage.setItem('ni.id', "");
                return;
            }
        }
		
        _currNetworkObjectId = dataItem.id;
        //As kendoTreeview.helper.js needs property named "selectedId"
        selectedId = _currNetworkObjectId;

        //Set sessionstorage to share across views
        sessionStorage.setItem('ni.name', dataItem.Name);
        sessionStorage.setItem('ni.type', dataItem.ItemType);
        sessionStorage.setItem('ni.id', dataItem.id);

        //Reload Menus
        var ddlMenuDS = $("#menuSelection").data("kendoComboBox").dataSource;
        ddlMenuDS.transport.options.read.data.netId = dataItem.id;
        ddlMenuDS.transport.options.read.data.networkType = dataItem.ItemType;
        ddlMenuDS.read();

        //reset DLL
        var ddlMenu = $("#menuSelection").data("kendoComboBox");
        ddlMenu.value("");
        //ddlMenu.text("Please select");

        var enableODSColumn = false;
        if(dataItem.Features != null && $.inArray( NetworkFeaturesSet.POSMapEnabled, dataItem.Features ) == 1)
        {
            enableODSColumn = true;
        }

        //Enable or Disable ODSColumn button
        if (parseInt(dataItem.ItemType) == NetworkTypes.Site && enableODSColumn){

            $('#btnViewODS').removeAttr("disabled");
            setAllGridsColumnVisibility("IsODSAvailable", true);
            //ddlMenu.enable(false);
        }
        else {
            $('#btnViewODS').attr("disabled", "disabled");
            setAllGridsColumnVisibility("IsODSAvailable", false);
            //ddlMenu.enable();
        }

        // clear and render the POS grid
        //$('#gridPOSData').html('');
        clearAllRows($("#gridPOSData"));
        //renderPOSDataGrid(_currNetworkObjectId);

        //Reset the Ids, Grid and Messages
        selectedMenuId = -1;
        clearAllGrids();
    //}
    //catch (e) {
    //    //alert(e);
    //}

}

function setAllGridsColumnVisibility(columnName, isVisible)
{
    setColumnVisibility($("#gridItemCat"), columnName, isVisible);
    setColumnVisibility($("#gridItemMod"), columnName, isVisible);
    setColumnVisibility($("#gridItemSub"), columnName, isVisible);
    setColumnVisibility($("#gridItemUpSell"), columnName, isVisible);
    setColumnVisibility($("#gridItemCSell"), columnName, isVisible);
    setColumnVisibility($("#gridItemCombo"), columnName, isVisible);

}
function clearAllGrids() {

    // set none as refreshed 
    _refreshedItemGrids = new Array();
    // clear rows
    clearAllRows($("#gridItemCat"));
    clearAllRows($("#gridItemMod"));
    clearAllRows($("#gridItemSub"));
    clearAllRows($("#gridItemUpSell"));
    clearAllRows($("#gridItemCSell"));
    clearAllRows($("#gridItemCombo"));
}
function clearAllRows(gridElement) {
    var grid = gridElement.data("kendoGrid");
    if (grid && grid.dataSource) {
        grid.dataSource.data([]);
    }
}

function setColumnVisibility(gridElement, columnName, isVisible) {
    var grid = gridElement.data("kendoGrid");
    if (grid) {
        if (isVisible)
        {
            grid.showColumn(columnName);
        } else
        {
            grid.hideColumn(columnName);
        }
    }
}
function tabSelected(gridElement) {
    $(".resizable-grid").removeClass("active").addClass('inactive');
    $(gridElement).find(".resizable-grid").removeClass("inactive").addClass('active');

    // render grid if it was not already rendered
    renderItemGrid(gridElement, _currNetworkObjectId);

    $(".k-grid").removeClass("active").addClass('inactive');
    $(gridElement).find(".k-grid").removeClass("inactive").addClass('active');
}

function reloadSelectedTabGrid() {
    // get the currently selected tab's grid id
    var gridElement = $('#tabItemGroup .active a').attr('href');
    renderItemGrid(gridElement, _currNetworkObjectId, true);

}

function renderItemGrid(callingElement, siteId, forceReload) {    
    if (selectedMenuId < 0) {
        setMainStatus(false, "Please select a Menu"); 
        return;
    }

    if (!(siteId && siteId > 0) || !(callingElement)) {
        return;
    }
    triggerResizeGrid();
    // determine parentGroupId from element name
    var gridDetails = getGridDetails(callingElement);

    // was grid rendered already? not found in array means not rendered
    if ($.inArray(gridDetails.itemParentGroupId, renderedItemGrids) == -1) {

        // add to rendered list
        renderedItemGrids.push(gridDetails.itemParentGroupId);

        // add to refreshed list
        _refreshedItemGrids.push(gridDetails.itemParentGroupId);
        
        triggerResizeGrid();
        // render
        gridDetails.gridElement.kendoGrid({
            dataSource: {
                type: "json",
                transport: {
                    read: {
                        url: "/datamap/MenuItems",
                        dataType: "json",
                        type: "GET",
                        contentType: "application/json; charset=utf-8",
                        data: { 'parentGroupId': gridDetails.itemParentGroupId, 'networkObjectId': siteId, "MenuId": selectedMenuId }
                    }
                },
                requestStart: function (e) {
                    if (e.type === "read") {
                        if (e.sender.transport.options.read.data.networkObjectId == "" || e.sender.transport.options.read.data.networkObjectId == 0) {
                            e.preventDefault();
                        }
                    }
                },
                pageSize: 100
            },
            groupable: true,
            sortable: true,
            resizable:true,
            filterable: {
                operators: {
                    string: { contains: "Contains", doesnotcontain: "Does Not Contain", eq: "Is Equal To", neq: "Is Not Equal To", startswith: "Starts With", endswith: "Ends With" }
                }
            },
            //resizable: true,
            pageable: {
                refresh: true,
                pageSizes: [100, 150, 200]
            },
            selectable: 'row',
            columns: [
                { field: "Id", title: "Id", hidden: true },
                { field: "IsAvailable", title: "IsAvailable", hidden: true },
                { field: "ParentName",  title: "Category / Collection" },
                { field: "DisplayName",  title: "Display Name" },
                { field: "ItemName",  title: "Item Name" },
                //{ field: "ItemPLU", width: 40, title: "Item PLU" }, // , template: '# if(ItemPLU == null) { #None# } else { # ${ ItemPLU } # } #' },
                { field: "MappedPOSDataId",  title: "MappedPOSDataId" , hidden : true},
                //{ field: "MapStatus", width: 60, title: "Map Status", template: '# if(ItemPLU == "") { ## } else if(MapStatus == "Manual") { #<img src="/Content/img/accept.png" /> ${MapStatus}# } else if (MapStatus == "Auto") { #<img src="/Content/img/tick.png" /> ${MapStatus}# } else { #<img src="/Content/img/exclamation.png" /> ${MapStatus}# } #' },
                { field: "MappedPLU", title: "POS Item", template: "<input value='#= MappedPOSDataId #' data-bind='value:MappedPOSDataId' data-role='dropdownlist' data-text-field='Text' data-value-field='Value' data-select='onMenuItemPOSSelect' data-itemId='${Id}' style='width: 100%' />" },
                //{ field: "MappedPOSItemName", width: 50, title: "Mapped Item" },
                { field: "IsODSAvailable", width: 110, title: "POS Available", htmlattributes: { style: 'text-align:"center"}' }, hidden: parseInt(sessionStorage.getItem('ni.type')) != NetworkTypes.Site, template: '# if(IsODSAvailable) { #<img src="/Content/img/accept.png" onclick="viewODSInfo(this)" data-toggle="tooltip" data-placement="bottom" data-title="test" data-trigger="manual" data-html=true/> # } else { #<img src="/Content/img/exclamation.png" /> # } #' }

               // @if (User.IsInRole("Administrator") || User.IsInRole("SuperAdministrator") || User.IsInRole("Editor"))
            //{
            // TASK - Performance improvement for POSMapping: Remove command button and added custom button while constructing row.
            // Enabled rows will have Disable button and Map or UnMap button
            // Disabled rows will have Enable button
            // Mapped Item will have UnMap button and vice versa
            //{ field: "Id", width: 100, filterable: false, sortable: false, title: "&nbsp;", template: "<a href='\\#' class='btn btn-default btn-xs btngridcommand' onclick='itemEnableBtnClick(#=Id#,#=IsEnabled#)' > # if(!IsEnabled) { # Enable # } else { # Disable #} #</a> # if(IsEnabled) { # <a href='\\#' class='btn btn-default btn-xs btngridcommand' onclick='manualMapBtnClick(#=Id#,#=MappedPLU#,\"" + "#=ItemName#" + "\",\"" + "#=MapStatus#" + "\")' > # if(MapStatus =='UnMapped') { #  Map  # } else { # UnMap #} #</a> #} #" },
	                //}
        ],
            //editable: true,
            //toolbar: ["save", "cancel"],
                    dataBound: function (e) {
                        scrollToTop(this);
                //// TASK - Performance improvement for POSMapping: Only row style is changed on databound
                //dataView = this.dataSource.view();
                //for (var i = 0; i < dataView.length; i++) {
                //    if (dataView[i].IsEnabled === false) {
                //        var uid = dataView[i].uid;
                //        var row1 = $('[data-uid=' + uid + ']');
                //        row1.addClass('rowgridItalic');
                //    }
                //}
                var grid = this;
                this.tbody.find('tr').each(function () {
                    var item = grid.dataItem(this);
                    if (item.IsOverride == true)
                    {
                        $(this).addClass('rowgridBoldItalic');
                    }
                    if (item.IsAvailable == false) {
                        $(this).addClass('rowgridItalic');
                    }
                    //if (item.MappedPOSDataId != 0) {
                    kendo.bind(this, item);

                    var ddl = $(this).find("[data-role=dropdownlist]").data("kendoDropDownList");
                    if (ddl != undefined) {
                        ddl.setDataSource(item.POSDataList);
                    }
                    //}
                });
            }              
        }).data("kendoGrid");
    }
    else {
        // grid was rendered earlier, now determine if a refresh is required
        var requiresReload = ($.inArray(gridDetails.itemParentGroupId, _refreshedItemGrids) == -1)
        if (requiresReload == true || forceReload == true) {

            var ds = gridDetails.gridElement.data("kendoGrid").dataSource;
            ds.transport.options.read.data.parentGroupId = gridDetails.itemParentGroupId;
            ds.transport.options.read.data.networkObjectId = siteId;
            ds.transport.options.read.data.MenuId = selectedMenuId;

            ds.page(1);

            // add to array if not present
            if (requiresReload) {
                _refreshedItemGrids.push(gridDetails.itemParentGroupId);
            }
        }
    }
    triggerResizeGrid();
// triggerGridsResize( 220, 90);
}
	
//function renderPOSDataGrid(networkObjectId) {

//	//if ($('#gridPOSData').html() != '') {
//	//    return;
//	//}
//	// was grid rendered already? not found in array means not rendered
//	if ($.inArray("gridPOSData", renderedItemGrids) == -1) {

//		// add to rendered list
//		renderedItemGrids.push("gridPOSData");

//		$("#gridPOSData").kendoGrid({
//			dataSource: {
//				type: "json",
//				transport: {
//					read: {
//						url: "/datamap/OperationalPOSData",
//						dataType: "json",
//						type: "GET",
//						contentType: "application/json; charset=utf-8",
//						data: { 'networkObjectId': networkObjectId }
//					}
//				},
//				pageSize: 100
//			},
//			pageable: {
//				refresh: true,
//				info: true
//			},
//			groupable: true,
//			sortable: true,
//			filterable: {
//				operators: {
//					string: { contains: "Contains", doesnotcontain: "Does Not Contain", eq: "Is Equal To", neq: "Is Not Equal To", startswith: "Starts With", endswith: "Ends With" }
//				}
//			},
//			resizable: true,
//			selectable: 'row',
//			columns: [
//			{ field: "POSDataId", width: 1, title: "Id", hidden: true },
//			{ field: "ScreenGroupName", width: 70, title: "Screen Group" },
//			{ field: "PLU", width: 50, type: "number", title: "PLU", format: "{0:0}" },
//			{ field: "ItemName", width: 90, title: "Item Name" },
//			{ field: "IsModifier", width: 50, title: "Is Modifier", type: "boolean" },
//			{ field: "IsSold", width: 50, title: "Is Sold", type: "boolean" },
//			{ field: "BasePrice", width: 50, title: "BasePrice", format: "{0:c}", type: "number" },
//			],
//			change: function () {
//				var selectedRow = this.select();
//				var rowData = this.dataItem(selectedRow);
//				$('#lblOPSPOSDataName').text(rowData.PLU + ' - ' + rowData.ItemName);
//				$('#inpOPSPOSDataId').val(rowData.POSDataId);

//			}
//		}).data("kendoGrid");
//	}
//	else {
		   
//		var ds = $("#gridPOSData").data("kendoGrid").dataSource;
//			ds.transport.options.read.data.networkObjectId = networkObjectId;

//			ds.page(1);
//			ds.read();
//	}
//}


//// TASK - Performance improvement for POSMapping: Change signature to pass id and value from grid row selected.
//function itemEnableBtnClick(id, isEnabled) {

//	if (id > 0) {
//	// save to database
//	$.ajax({
//		url: '/datamap/ItemStatus',
//		type: 'POST',
//			data: { 'networkObjectId': _currNetworkObjectId, 'itemId': id, 'menuId': selectedMenuId, 'isEnabled': !isEnabled }, // If it is enabled earlier, pass false to disable and vice versa
//		success: function (data, textStatus, jqXHR) {

//			statusMessage(data.status, data.message);

//			if (data.status == true) {
//				reloadSelectedTabGrid();
//			}
//		},
//		error: function (xhr) {
//			statusMessage("Error", "Internal server error");
//		},
//		beforeSend: function () {
//			statusMessage("Info", "Please wait...", null, null, null, false);
//		},
//	});
//	}

//}
//// TASK - Performance improvement for POSMapping: passed values from grid row selected
//function manualMapBtnClick(id, MappedPLU, ItemName, mapStatus) {

//	//if the status is Unmapped, Map the Item.
//	if (mapStatus == "UnMapped") {

//		// open map window
//		//Avoid problem which occurs with ie after selecting another Site.
//		if (id > 0) {
//			// fill popup window controls
//			$('#inpItemId').val(id);
//			$('#lblPOSDataName').text(MappedPLU);
//			$('#posDataMapHeader').html('<p>Select a PLU for item <b>' + ItemName + '</b>...</p>');
//			// open the window

//			$("#gridPOSData").css('height', 500);
//			$("#gridPOSData .k-grid-content").css('height', 410);
//			$("#windowPOSDataMap").data("kendoWindow").center().open();
//		}

//	}
//	else {

//		if (id > 0) {
//			confirmWindow("Confirm", 'Delete POS mapping for ' + ItemName + ' ?', function () { }, "400px", "Yes", "No", function (result) {
//				if (result == true) {

//					// save to database
//					$.ajax({
//						url: '/datamap/POSManualUnmap',
//						type: 'POST',
//									data: { 'networkObjectId': _currNetworkObjectId, 'itemId': id, 'MenuId': selectedMenuId },
//						success: function (data, textStatus, jqXHR) {

//							statusMessage(data.status, data.message);

//							if (data.status == true) {

//								reloadSelectedTabGrid();
//							}

//						},
//						error: function (xhr) {
//							statusMessage("Error", "Internal server error");
//						},
//						beforeSend: function () {
//							statusMessage("Info", "Please wait...", null, null, null, false);
//						},
//					});
//				}
//			});
//		}

//	}
//}

function viewODSInfo(e) {
    var gridElement = $(e).closest(".k-grid");
    var row = $(e).closest("tr");
    var grid = gridElement.data("kendoGrid");
    var dataItem = grid.dataItem(row);
    if(dataItem != null)
    {
        var InsertedDate = new Date(parseInt(dataItem.ODSData.InsertedDate.substr(6)));

        var tooltipText = 'Item : ' + dataItem.ODSData.ItemName
                    + '<br/> PLU : ' + dataItem.ODSData.PLU
                    + '<br/> Price : ' + dataItem.ODSData.BasePrice
                    + '<br/> Group : ' + dataItem.ODSData.ScreenGroupName
                    + '<br/> Date : ' + InsertedDate.toLocaleString();

        //$(e).tooltip({ placement: 'bottom', trigger : 'click',
        //    title: 'Item : ' + dataItem.ODSData.ItemName
        //            + '<br/> PLU : ' + dataItem.ODSData.PLU
        //            + '<br/> Price : ' + dataItem.ODSData.BasePrice
        //            + '<br/> Group : ' + dataItem.ODSData.ScreenGroupName
        //            + '<br/> Date : ' + InsertedDate.toLocaleString(),
        //    html: true });
        $(e).attr('data-original-title', tooltipText)
            .tooltip('fixTitle')
        $(e).tooltip('toggle');
    }
}

function changeMenuItemPOS(itemId, posdataId, networkobjectId, menuid,callback) {
    $.ajax({
        url: '/DataMap/ChangePOSItem',
        type: 'POST',
        dataType: 'json',
        async: false,
        data: { posItemId: posdataId, itemId: itemId, networkobjectId: networkobjectId, menuId: menuid },
        success: function (data, textStatus, jqXHR) {
            statusMessage(data.status, data.lastActionResult);

            callback(data.status);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            if (xhr.status == 401) {
                statusMessage(false, thrownError);
            }
            else {
                statusMessage(false, "Unexpected error occured while changing POS.");
            }
            callback(false);
        }
    });
}

function onMenuItemPOSSelect(e) {
    var ddlElement = $(e.sender.wrapper).find("[data-role=dropdownlist]");
    var itemId = ddlElement.attr("data-itemId");
    var posItem = this.dataItem(e.item.index()); //this will have the new selected value

    var currentPOSItem = this.value(); // this will have the old selected value

    //If value is changed
    if (itemId != undefined && posItem != undefined && currentPOSItem != posItem.Value) {

        changeMenuItemPOS(itemId, posItem.Value, _currNetworkObjectId, selectedMenuId , function (result) {
            if (result == false) {
                e.preventDefault();
            }
            else {
                reloadSelectedTabGrid();
            }
        });
    }
    else
    {
        e.preventDefault();
    }

}
////Resize Kendo TreeView And/Or Kendo Grid
$(window).load(function () { triggerResize(); });
$(window).resize(function () { triggerResize(); });
