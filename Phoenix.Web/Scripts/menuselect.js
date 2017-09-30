
// defined function to add hover effect and remove it when row is clicked
function menuGridDataBound(e) {
    scrollToTop(this);

    //Disable Create button until validate Network is selected
    var grid = e.sender;
    
    if (selectedNodeType < 2) {
        var createButton = grid.wrapper.find(" .k-grid-add");
        createButton.addClass('disabledAnchor').addClass("k-state-disabled").removeClass("k-grid-add");
    }
    else {
        var createButton = grid.wrapper.find(" .k-state-disabled");
        createButton.removeClass('disabledAnchor').removeClass("k-state-disabled").addClass("k-grid-add");
    }

    //Selects all edit buttons
    //$("#gridMenu tbody tr .k-grid-edit").each(function () {
    grid.tbody.find("tr .k-grid-edit").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        //Check in the current dataItem if the row is editable
        if (currentDataItem.IsEditable === false) {
           // $(this).remove();
        }

    })

    //Selects all delete buttons
    //$("#gridMenu tbody tr .k-grid-customD").each(function () {
    grid.tbody.find("tr .k-grid-customD").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        //Check in the current dataItem if the row is deletable
        if (currentDataItem.IsDeletable === false) {
            $(this).addClass("k-state-disabled");
        }
        else
        {
            $(this).removeClass("k-state-disabled");
        }
    })

    //Selects all delete buttons
    //$("#gridMenu tbody tr .k-grid-revert").each(function () {
    grid.tbody.find("tr .k-grid-revert").each(function () {
        var currentDataItem = grid.dataItem($(this).closest("tr"));

        //Check in the current dataItem if the row is deletable
        if (currentDataItem.IsMenuOverriden === false) {
            $(this).remove();
        }
    })

    $("table.k-focusable tbody tr").hover(
      function () {
          $(this).toggleClass("k-state-hover");
      }
    );

    triggerResizeGrid();
}
    function showMessage(container, name, errors) {
        //add the validation message to the form
        container.find("[data-valmsg-for=" + name + "]")
            .replaceWith($(validationMessageTmpl({ field: name, message: errors[0] })))
    }

    // Menu edit
    function showMenuDetails(e) {
        e.preventDefault();

        var dataItem = this.dataItem($(e.currentTarget).closest("tr"));

        document.location = '/menu/MenuEdit/' + dataItem.MenuId + '?netId=' + selectedId;

    }
    function copyMenuDetails(e) {
        e.preventDefault();
        _IsActionMenuCopy = true;
        var selectedRow = $(e.currentTarget).closest("tr");
        this.editRow(selectedRow);
        //currItem = this.dataItem($(e.currentTarget).closest("tr"));

        //if (currItem != null && currItem != undefined) {
        //    confirmWindow("Confirm", "Are you sure you want to Copy " + currItem.InternalName + " Menu?", function () { }, "400px", "Ok", "Cancel", function (data) {
        //        if (data === true) {
        //            //alertWindow("", "Not implemented.", function () { })

        //            $.ajax({
        //                url: '/Menu/CopyMenu',
        //                type: 'Get',
        //                dataType: 'json',
        //                data: { 'menuId': currItem.MenuId, 'netId': selectedId },
        //                beforeSend: function (e) {
        //                    $.blockUI({ message: "<div style='margin:10px 0 10px 0;'><b><img src='/Images/loading.gif' /> Please wait...</b></div>", overlayCSS: { backgroundColor: "transparent" } })
        //                },
        //                success: function (data, textStatus, jqXHR) {
        //                    $.unblockUI();
        //                    statusMessage(data.status, data.lastActionResult);

        //                    if (data.lastActionResult.indexOf("failed") != -1) return;
        //                    var grid = $("#gridMenu").data("kendoGrid").dataSource;
        //                    grid.page(1);
        //                    grid.read();
        //                },
        //                error: function (xhr) {

        //                }
        //            });
        //        }
        //    });
        //}
    }

    // Menu delete
    function deleteMenu(e) {
        e.preventDefault();

        currItem = this.dataItem($(e.currentTarget).closest("tr"));

        if (currItem != null && currItem != undefined) {
            confirmWindow("Delete confirm", "Are you sure you want to delete " + currItem.InternalName + " Menu?", function () { }, "400px", "Ok", "Cancel", function (data) {
                if (data === true) {
                    //alertWindow("", "Not implemented.", function () { })

                    $.ajax({
                        url: '/Menu/DeleteMenu',
                        type: 'POST',
                        dataType: 'json',
                        data: { 'model': JSON.stringify(currItem), 'netId': selectedId },
                        beforeSend: function (e) {
                            showWaitSpinner();
                        },
                        success: function (data, textStatus, jqXHR) {
                            hideWaitSpinner();
                            statusMessage(data.status, data.lastActionResult);

                            if (data.lastActionResult.indexOf("failed") != -1) return;
                            var grid = $("#gridMenu").data("kendoGrid").dataSource;
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

    // Menu Revert
    function revertMenu(e) {
        e.preventDefault();

        currItem = this.dataItem($(e.currentTarget).closest("tr"));

        if (currItem != null && currItem != undefined) {
            confirmWindow("Confirm", "Revert all the changes in the " + currItem.InternalName + " Menu?", function () { }, "400px", "Ok", "Cancel", function (data) {
                if (data === true) {
                    //alertWindow("", "Not implemented.", function () { })

                    $.ajax({
                        url: '/Menu/RevertMenu',
                        type: 'Get',
                        dataType: 'json',
                        data: { 'menuId': currItem.MenuId, 'netId': selectedId },
                        beforeSend: function (e) {
                            showWaitSpinner();
                        },
                        success: function (data, textStatus, jqXHR) {
                            hideWaitSpinner();
                            statusMessage(data.status, data.lastActionResult);

                            if (data.lastActionResult.indexOf("failed") != -1) return;
                            var grid = $("#gridMenu").data("kendoGrid").dataSource;
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

function restoreSelectionSub() {
    try {
        selectedId = sessionStorage.SelectedTreeNodeId_Menu;
    }
    catch (e) {
        alert(e);
    }
}

function storeTreeNodeExpansionNSelectionSub(selectedNode) {
    try {
        sessionStorage.ExpansionSequence_Menu = _expansionSequence;
        sessionStorage.SelectedTreeNodeId_Menu = selectedId;
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

        var grdmenu = $("#gridMenu").data("kendoGrid").dataSource;
        grdmenu.transport.options.read.data.netId = selectedId;
        grdmenu.page(1);
        grdmenu.read();
    }
    catch (e) {
        alert(e);
    }
}

//refresh menu on tag close
function onWinClose(e) {
}

//var cities = ["Seattle", "Tacoma", "Kirkland", "Redmond", "London", "Philadelphia", "New York",
//    "Seattle", "London", "Boston"];
//function channelsEditor(container, options) {
//    $("<select multiple='multiple' " +
//      "data-bind='value : Channels'/>").appendTo(container).kendoMultiSelect({
//          dataSource: cities
//      });
//}

//Resize Kendo TreeView And/Or Kendo Grid
$(window).load(function () { triggerResizeTree(); });
$(window).resize(function () { triggerResize(); });