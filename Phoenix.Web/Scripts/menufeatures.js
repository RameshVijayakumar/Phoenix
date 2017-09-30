
//Notice delete
$("#btnNoticeDelete").click(function () {
    setStatus($('#operationStatus'));

    var grid = $("#gridSpecialNotice").data("kendoGrid");

    var noticesChecked = grid.tbody.find(":checkbox:checked");
    var noticesCheckedCnt = noticesChecked.length;

    if (noticesCheckedCnt == 0) {
        statusMessage("Info", "Select one or more notices to delete");
        return;
    }

    var noticeList = new Array();
    noticesChecked.each(function () {
        noticeList.push(grid.dataItem($(this).closest("tr")));
    });
    
    confirmWindow("Delete confirm", "Are you sure you want to delete the selected special notice(s)?", function () { }, "400px", "OK", "Cancel", function (data) {
        if (data === true) {
            deleteNotices(noticeList);
        }
    });
});

// Notice delete
function deleteNotice(e) {
    e.preventDefault();
    var dataItem = this.dataItem($(e.currentTarget).closest("tr"));

    var confirmationMsg = "Are you sure you want to delete the special notice: '" + dataItem.NoticeName + "' ?";
    if (dataItem.IsLinkedToMenu) {
        confirmationMsg = "This special notice: '" + dataItem.NoticeName + "' is linked with menu(s).";
        confirmationMsg += "<br/> Are you sure you want to delete this special notice?";
    }

    confirmWindow("Delete confirm", confirmationMsg, function () { }, "400px", "OK", "Cancel", function (data) {
        if (data === true) {
            //This approach is followed to make use of existing methods:  deleteNotices & '/SpecialNotice/DeleteSpecialNotices' methods
            var selectedIds = new Array();
            selectedIds.push(dataItem);
            deleteNotices(selectedIds);
        }
    });
}

function deleteNotices(noticeIdList) {
            $.ajax({
                url: '/SpecialNotice/DeleteSpecialNotices',
                type: 'POST',
                dataType: 'json',
                data: { noticeIds: JSON.stringify(noticeIdList) },
                success: function (data, textStatus, jqXHR) {
                    if (data.status) {
                        refreshNotices();
                    }
                    statusMessage(data.status, data.lastActionResult);
                },
                error: function (xhr, ajaxOptions, thrownError) {
                    statusMessage(false, thrownError);
                }
            });
}

//refresh Notices
function refreshNotices() {
    var grid = $("#gridSpecialNotice").data("kendoGrid").dataSource;
    grid.transport.options.read.data.networkObjectId = selectedId;
    grid.page(1);
    grid.read();
}

function spRequestEnd(e) {
    if (e.type != "read") {
        //Check is the response contains Errors
        if (e.response != undefined && e.response.Errors != undefined) {
        }
        else {
            if (e.type === "update") {
                var name = e.response.NoticeName;
                if (name == undefined) {
                    name = "";
                }
                statusMessage(true, "Updated the Special Notice " + name);
            }
            else if (e.type === "create") {
                var name = e.response.Name;
                if (name == undefined) {
                    name = "";
                }
                statusMessage(true, "Created the Special Notice " + name);
            }
            var grid = $("#gridSpecialNotice").data("kendoGrid").dataSource;
            grid.read();
        }
    }
}




//Flag delete
$("#btnFlagDelete").click(function () {

    var grid = $("#gridModifierFlag").data("kendoGrid");

    var rowsChecked = grid.tbody.find(":checkbox:checked");
    var rowsCheckedCnt = rowsChecked.length;

    if (rowsCheckedCnt == 0) {
        statusMessage("Info", "Select one or more modifier flags to delete");
        return;
    }

    var flagList = new Array();
    rowsChecked.each(function () {
        flagList.push(grid.dataItem($(this).closest("tr")));
    });
    
    confirmWindow("Delete confirm", "Are you sure you want to delete the selected modifier flag(s)?", function () { }, "400px", "OK", "Cancel", function (data) {
        if (data === true) {
            deleteFlags(flagList);
        }
    });
});

// Flag delete
function deleteFlag(e) {
    e.preventDefault();
    var dataItem = this.dataItem($(e.currentTarget).closest("tr"));

    var confirmationMsg = "Are you sure you want to delete the modifier flag: '" + dataItem.Name + "' ?";

    confirmWindow("Delete confirm", confirmationMsg, function () { }, "400px", "OK", "Cancel", function (data) {
        if (data === true) {
            var selectedIds = new Array();
            selectedIds.push(dataItem);
            deleteFlags(selectedIds);
        }
    });
}

function deleteFlags(flagList) {
            $.ajax({
                url: '/ModifierFlag/DeleteModifierFlags',
                type: 'POST',
                dataType: 'json',
                data: { flags: JSON.stringify(flagList) },
                success: function (data, textStatus, jqXHR) {
                    if (data.status) {
                        refreshFlags();
                    }
                    statusMessage(data.status, data.lastActionResult);
                },
                error: function (xhr, ajaxOptions, thrownError) {
                    statusMessage(false, thrownError);
                }
            });
}
function saveMFlag(e) {
    e.preventDefault();
    var dataItem = this.dataItem($(e.currentTarget).closest("tr"));

    $.ajax({
        url: '/ModifierFlag/SaveModifierFlag',
        type: 'POST',
        dataType: 'json',
        data: { model: dataItem,NetworkObjectId : selectedId },
        success: function (data, textStatus, jqXHR) {
            if (data.status) {
                refreshFlags();
            }
            statusMessage(data.status, data.lastActionResult);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            statusMessage(false, thrownError);
        }
    });
}

//refresh Flags
function refreshFlags() {
    var grid = $("#gridModifierFlag").data("kendoGrid").dataSource;
    //grid.transport.options.read.url = '/ModifierFlag/GetModifierFlagList?networkObjectId=' + selectedId;
    grid.read();
}

function refreshSelectedTab(callingElement) {
    if (callingElement == '#1A') {
        refreshNotices();
    }
    else if (callingElement == '#1B') {
        refreshFlags();
    }
}

function mfError(e) {
    if (e.errors) {
        grid = $("#gridModifierFlag").data("kendoGrid");
        grid.one("dataBinding", function (e) {
            e.preventDefault();   // cancel grid rebind if error occurs                             
        });
        for (var error in e.errors) {
            showMessage(grid.editable.element, error, e.errors[error].errors);
        }
    }
}
function mfRequestEnd(e) {
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
                statusMessage(true, "Updated the Modifier Flag " + name);
            }
            else if (e.type === "create") {
                var name = e.response.Name;
                if (name == undefined) {
                    name = "";
                }
                statusMessage(true, "Created the Modifier Flag " + name);
            }
            var grid = $("#gridModifierFlag").data("kendoGrid").dataSource;
            grid.read();
        }
    }
}
function mfAdditionalData(e) {
        e.NetworkObjectId = selectedId
        return e;
}
// databound for both splNotice and MFlags grid
function gridDataBound(e) {
    scrollToTop(this);
    $("table.k-focusable tbody tr").hover(function () {
        $(this).toggleClass("k-state-hover");
    });

    var headerCheckBox = this.thead.find(":checkbox")[0]
    if (headerCheckBox != undefined) {
        headerCheckBox.checked = false;
    }

    var grid = e.sender;
    if (!_isValidTreeNodeSelected) {
        var createButton = grid.wrapper.find(" .k-grid-add");
        createButton.addClass('disabledAnchor').addClass("k-state-disabled").removeClass("k-grid-add");
    }
    else {
        var createButton = grid.wrapper.find(" .k-state-disabled");
        createButton.removeClass('disabledAnchor').removeClass("k-state-disabled").addClass("k-grid-add");
    }
    triggerResizeGrid();
};

function mfEdit(e) {
    e.container.find('[name="Name"]').focus();
}
