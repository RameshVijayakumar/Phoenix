 var valueDelimiter = "|";

 $(function () {
     highlightMenubar("admin", "channel");
 });
 function onGridSchCycleDataBound(e) {
     scrollToTop(this);
     var grid = e.sender;
    if (grid != undefined) {
        //var cyclelist = grid.dataSource._data;
        //var cycleListCount = cyclelist.length;
        var cyclelistString = "";
        //if (cycleListCount > 0) {
        //    for (var loopCntr = 0; loopCntr < cycleListCount; loopCntr++) {
        //        cyclelistString += valueDelimiter + cyclelist[loopCntr].SchCycleId + valueDelimiter + cyclelist[loopCntr].CycleName.toUpperCase();
        //    }
        //    sessionStorage.CycleGridData = cyclelistString + valueDelimiter;

        //}
        dataView = this.dataSource.view();
        if (dataView.length > 0) {
            for (var i = 0; i < dataView.length; i++) {
                cyclelistString += valueDelimiter + dataView[i].SchCycleId + valueDelimiter + dataView[i].CycleName.toUpperCase();

                if (dataView[i].IsActive === false) {
                    var uid = dataView[i].uid;
                    $("#gdSchCycles tbody").find("tr[data-uid=" + uid + "]").addClass("disableRowClass");
                }
            }
            sessionStorage.CycleGridData = cyclelistString + valueDelimiter;
        }
    }
    //Disable Create button until validate Network is selected

    if (selectedNodeType < 1) {
        var createButton = grid.wrapper.find(" .k-grid-add");
        createButton.addClass('disabledAnchor').addClass("k-state-disabled").removeClass("k-grid-add");
    }
    else {
        var createButton = grid.wrapper.find(" .k-state-disabled");
        createButton.removeClass('disabledAnchor').removeClass("k-state-disabled").addClass("k-grid-add");
    }

    //Selects all edit buttons
    grid.tbody.find("tr .k-grid-edit").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        ////Check in the current dataItem if the row is editable
        //if (currentDataItem.IsCreatedAtThisNetwork === false) {
        //    $(this).remove();
        //}
        if (currentDataItem.IsActive === false || currentDataItem.IsCreatedAtThisNetwork === false) {
            $(this).addClass('disabledAnchor').addClass("k-state-disabled");
        }
        else {
            $(this).removeClass('disabledAnchor').removeClass("k-state-disabled");
        }
    })
     //Selects all delete buttons
    grid.tbody.find("tr .k-grid-customD").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        //if (currentDataItem.IsOverride === true) {
        //    $(this).html("Disable");
        //}
        //else if (currentDataItem.IsCreatedAtThisNetwork === false) {
        //    $(this).html("Disable");
        //}
        //else {
        //    $(this).html("Delete");
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
    triggerResizeGrid();
}
function deleteCycle(e) {
    e.preventDefault();

    var grid = this;
    var row = $(e.currentTarget).closest("tr");
    var dataItem = grid.dataItem(row);
    if (row != null && row != undefined) {

        confirmWindow("Delete confirm", "Are you sure you want to delete " + dataItem.CycleName + " cycle?", function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {
                $.ajax({
                    url: '/Schedule/DestroyCycle',
                    type: 'GET',
                    dataType: 'json',
                    data: { 'model': JSON.stringify(dataItem), 'netId': selectedId },
                    beforeSend: function (e) {
                        showWaitSpinner();
                    },
                    success: function (data, textStatus, jqXHR) {
                        hideWaitSpinner();
                        statusMessage(data.status, data.lastActionResult);

                        if (data.lastActionResult.indexOf("failed") != -1) return;
                        var grid = $("#gdSchCycles").data("kendoGrid").dataSource;
                        grid.page(1);
                        grid.read();
                    },
                    error: function (xhr) {

                    }
                });
            }
        });
    }
}

function disableCycle(e) {
    e.preventDefault();

    var grid = this;
    var row = $(e.currentTarget).closest("tr");
    var dataItem = grid.dataItem(row);
    if (row != null && row != undefined) {

        confirmWindow("Disable confirm", "Are you sure you want to disable " + dataItem.CycleName + " cycle?", function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {
                $.ajax({
                    url: '/Schedule/DisableCycle',
                    type: 'POST',
                    dataType: 'json',
                    data: { 'model': JSON.stringify(dataItem), 'netId': selectedId },
                    beforeSend: function (e) {
                        showWaitSpinner();
                    },
                    success: function (data, textStatus, jqXHR) {
                        hideWaitSpinner();
                        statusMessage(data.status, data.lastActionResult);

                        if (data.lastActionResult.indexOf("failed") != -1) return;
                        var grid = $("#gdSchCycles").data("kendoGrid").dataSource;
                        grid.page(1);
                        grid.read();
                    },
                    error: function (xhr) {

                    }
                });
            }
        });
    }
}

function enableCycle(e) {
    e.preventDefault();

    var grid = this;
    var row = $(e.currentTarget).closest("tr");
    var dataItem = grid.dataItem(row);
    if (row != null && row != undefined && dataItem.IsActive == false) {
        confirmWindow("Enable confirm", "Are you sure you want to enable " + dataItem.CycleName + " cycle?", function () { }, "400px", "Ok", "Cancel", function (data) {
            if (data === true) {
                $.ajax({
                    url: '/Schedule/EnableCycle',
                    type: 'POST',
                    dataType: 'json',
                    data: { 'model': JSON.stringify(dataItem), 'netId': selectedId },
                    beforeSend: function (e) {
                        showWaitSpinner();
                    },
                    success: function (data, textStatus, jqXHR) {
                        hideWaitSpinner();
                        statusMessage(data.status, data.lastActionResult);

                        if (data.lastActionResult.indexOf("failed") != -1) return;
                        var grid = $("#gdSchCycles").data("kendoGrid").dataSource;
                        grid.page(1);
                        grid.read();
                    },
                    error: function (xhr) {

                    }
                });
            }
        });
    }
}

function editCycle(e) {

    var grid = this;
    var row = $(e.currentTarget).closest("tr");

    if (row != null && row != undefined) {
        grid.editRow(row);
    }
}

function checkForUniquenessOfCycleName(enteredCycleName, rowId) {
    var returnValue = false;
    var cycleList = sessionStorage.CycleGridData;
    //Ignore the substring which is with same RowId
    var subStrWithRowId = valueDelimiter + rowId + valueDelimiter;
    var subStrWithRowIdCharLength = subStrWithRowId.length;

    var indexOfSubStrWithRowId = cycleList.indexOf(subStrWithRowId);
    var indexOfFollowingValueDelimiter = cycleList.indexOf(valueDelimiter, indexOfSubStrWithRowId + subStrWithRowIdCharLength);
    var subStringVd_Id_Vd_Name = cycleList.substring(indexOfSubStrWithRowId, indexOfFollowingValueDelimiter);
    cycleList = cycleList.replace(subStringVd_Id_Vd_Name, '');//Vd is ValueDelimiter
    cycleList += valueDelimiter;
    returnValue = cycleList.indexOf(valueDelimiter + enteredCycleName.toUpperCase() + valueDelimiter) == -1;
            
    return returnValue;
}

function restoreSelectionSub() {
    try {
        selectedId = sessionStorage.SelectedTreeNodeId_SchCycle;
    }
    catch (e) {
        alert(e);
    }
}

function storeTreeNodeExpansionNSelectionSub(selectedNode) {
    try {
        sessionStorage.ExpansionSequence_SchCycle = _expansionSequence;
        sessionStorage.SelectedTreeNodeId_SchCycle = selectedId;
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
        //alert("selid=" + selectedId);

        var gridSchCycles = $("#gdSchCycles").data("kendoGrid").dataSource;
        gridSchCycles.transport.options.read.data.netId = selectedId;
        gridSchCycles.page(1);
        gridSchCycles.read();
    }
    catch (e) {
        alert(e);
    }
}

// Not in Use
function onSortChange(e) {

    var gridSchCycles = $("#gdSchCycles").data("kendoGrid");
    var skip = gridSchCycles.dataSource.skip(),
                        oldIndex = e.oldIndex + skip,
                        newIndex = e.newIndex + skip,
                        data = gridSchCycles.dataSource.data(),
                        dataItem = gridSchCycles.dataSource.getByUid(e.item.data("uid"));

    gridSchCycles.dataSource.remove(dataItem);
    gridSchCycles.dataSource.insert(newIndex, dataItem);

    $.ajax({
        url: '/Schedule/MoveCycle',
        type: 'POST',
        dataType: 'json',
        data: { 'model': JSON.stringify(dataItem), 'netId': selectedId, 'newSortOrder': newIndex, 'oldSortOrder': oldIndex },
        beforeSend: function (e) {
            //$.blockUI({ message: "<div style='margin:10px 0 10px 0;'><b><img src='/Images/loading.gif' /> Please wait...</b></div>", overlayCSS: { backgroundColor: "transparent" } })
        },
        success: function (data, textStatus, jqXHR) {
            //$.unblockUI();
            statusMessage(data.status, data.lastActionResult);

            if (data.lastActionResult.indexOf("failed") != -1) return;
            var grid = $("#gdSchCycles").data("kendoGrid").dataSource;
            grid.page(1);
            grid.read();
        },
        error: function (xhr) {

        }
    });
}

function schCycleError(e) {
    if (e.errors) {
        grid = $("#gdSchCycles").data("kendoGrid");
        grid.one("dataBinding", function (e) {
            e.preventDefault();   // cancel grid rebind if error occurs                             
        });
        for (var error in e.errors) {
            showMessage(grid.editable.element, error, e.errors[error].errors);
        }
    }
}
function schCycleRequestEnd(e) {
    if (e.type != "read") {
        //Check is the response contains Errors
        if (e.response != undefined && e.response.Errors != undefined) {
        }
        else {
            if (e.type === "update") {
                var name = e.response.Name;
                if (name == undefined) {
                    name = "";
                }
                statusMessage(true, "Updated the Cycle " + name);
            }
            else if (e.type === "create") {
                var name = e.response.Name;
                if (name == undefined) {
                    name = "";
                }
                statusMessage(true, "Created the Cycle " + name);
            }
            var grid = $("#gdSchCycles").data("kendoGrid").dataSource;
            grid.read();
        }
    }
}
function schCycleAdditionalData(e) {
    e.NetworkObjectId = selectedId
    e.netId = selectedId
    return e;
}

function schCycleEdit(e) {
    e.container.find('[name="Name"]').focus();
}
//Resize Kendo TreeView And/Or Kendo Grid
$(window).load(function () { triggerResizeTree(); });
$(window).resize(function () { triggerResize(); });
