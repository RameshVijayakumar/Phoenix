var DayofWeek = {
    Sunday: 1,
    Monday: 2,
    Tuesday: 3,
    Wednesday: 4,
    Thrusday: 5,
    Friday: 6,
    Saturday: 7
};
var cycdataModified = false;
var timeModified = false;
var _hasTimeAlreadyDefined = false;
var copiedStartTime = "____";
var copiedEndTime = "____";
$("#btnScheduleList").click(function () {
    var redirectURL = '/Schedule/Index';
    window.location = redirectURL.replace(/\&amp;/g, '&');
});

//$("#btnManageCycle").click(function () {
//    cycdataModified = false;
//    var win = $("#wnManageCycle").data("kendoWindow");
//    win.center().open();
//});
$("#btnSaveSch").click(function () {
    var isvalid = true;

    formValidation(function (result) {
        isvalid = result;
    })

    if (isvalid) {

        $("#formErr").html("");
        var form = $('#frmSchedule');
        var validator = form.validate({ ignore: ".ignore" });

        if (form.valid()) {
            $("#SaveSchedule").val(true);
            $(".functionalbtn").addClass('disabledAnchor');
            $(".functionalbtn").attr('disabled', 'disabled');
            var form = $('#frmSchedule');
            form.submit();
            $("#btnSaveSch").button('loading');
        }
    }
});

$("#btnSaveTime").click(function () {
    if (timeModified) {
        var isTimeValid = true;
        if ($("#j_start_time").val() != "" && $("#j_end_time").val() != "" && $("#j_start_time").val() == $("#j_end_time").val()) {
            setStatus($('#schoperationStatus'), false, "Start and End times cannot be same.");
            isTimeValid = false;
        }
        else {
            if (_hasTimeAlreadyDefined) {
                if ($("#j_start_time").val() == "" || $("#j_end_time").val() == "") {
                    confirmWindow("Remove confirm", "This may remove the associated data for this. Are you sure you want to remove the Times? ", function () { }, "400px", "Ok", "Cancel", function (data) {
                        if (data === true) {
                            isTimeValid = saveTimeChangestoModel();
                        }
                    });
                }
                else {
                    isTimeValid = saveTimeChangestoModel();
                }
            }
            else {
                isTimeValid = saveTimeChangestoModel();
            }
            if (isTimeValid) {
                $("#wnSelectTime").data("kendoWindow").close();
            }
        }
    }
    else {
        $("#wnSelectTime").data("kendoWindow").close();
    }
});

$("#btnCancel").click(function () {
    $("#wnSelectTime").data("kendoWindow").close();
});

$(function () {

    highlightMenubar("schedule");
    $("#SchName").focus();
    $("#btnSaveSch").button('reset');
    $('#txtAltDesc').val($('#hdnDesc_0').val());
    if ($('#statusMessage').val() != "") {
        var msg = $('#statusMessage').val();
        if (msg.length > 0) {
            if (msg.indexOf("failed") != -1) {
                statusMessage(false, msg);
            }
            else {
                statusMessage(true, msg);
            }
        }
    }
    $("#btnSaveSch").tooltip({ placement: 'bottom', trigger: 'manual', title: $('#divNotSavedMsg').html() });

    if ($("#SaveSchedule").val() == "false") {
        //$('#btnManageCycle').attr('disabled', 'disabled');
        //$('#divNotSavedMsg').show();
        $("#btnSaveSch").tooltip('show');
    }
    else {
        //$('#btnManageCycle').removeAttr('disabled');
        //$('#divNotSavedMsg').hide();
        $("#btnSaveSch").tooltip('hide');
    }
    var sdDatetimePicker = $("#j_start_date").kendoDateTimePicker({
        //value: new Date($("#StartDate").val()),
        change: function () {
            $("#StartDate").val($("#j_start_date").val())
        }
    }).data("kendoDateTimePicker");

    if ($("#StartDate").val() != null && $("#StartDate").val().trim() != '') {
        sdDatetimePicker.value(new Date($("#StartDate").val()));
    }
    var edDatetimePicker = $("#j_end_date").kendoDateTimePicker({
        //value: new Date($("#EndDate").val()),
        change: function () {
            $("#EndDate").val($("#j_end_date").val())
        }
    }).data("kendoDateTimePicker");

    if ($("#EndDate").val() != null && $("#EndDate").val().trim() != '') {
        edDatetimePicker.value(new Date($("#EndDate").val()));
    }
    $("#j_start_time").change(function (e) {
        setStatus($('#schoperationStatus'));
        timeModified = true;
    });

    $("#j_end_time").change(function (e) {
        setStatus($('#schoperationStatus'));
        timeModified = true;
    });

    $("#j_start_time").kendoTimePicker(
        {
            height: "350",
            format: "h:mmtt",
            change: function () {
                setStatus($('#schoperationStatus'));
                timeModified = true;
            }
        });
    $("#j_end_time").kendoTimePicker(
        {
            height: "350",
            format: "h:mmtt",
            change: function () {
                setStatus($('#schoperationStatus'));
                timeModified = true;
            }
        });

    $("#wnSelectTime").kendoWindow({
        modal: true,
        width: "400px",
        height: "160px",
        title: "Select Time",
        visible: false,
        close: function (e) {

        }, activate: function (e) {
            $('#j_start_time').select();
        }
    }).data("kendoWindow");

    $("#gdScheduleDetails").kendoGrid({
        dataSource: list2,
        dataBound: onDataBound,
        columns: [
            { field: "CycleName", width: 60, title: " " },
            { field: "SunST", width: 90, title: "Sunday", template: "<span id='GSchSummary_#=SchCycleId#_SunST'>#if(SunST !=null) { # #=formattedTime(SunST.Hours, SunST.Minutes)# #}#</span><span id='GSchSummary_#=SchCycleId#_SunET'># if(SunET !=null) { # - #=formattedTime(SunET.Hours, SunET.Minutes)# #}#</span>" },
            { field: "MonST", width: 90, title: "Monday", template: "<span id='GSchSummary_#=SchCycleId#_MonST'>#if(MonST !=null) { # #=formattedTime(MonST.Hours, MonST.Minutes)# #}#</span><span id='GSchSummary_#=SchCycleId#_MonET'># if(MonET !=null) { # - #=formattedTime(MonET.Hours, MonET.Minutes)# #}#</span>" },
            { field: "TueST", width: 90, title: "Tuesday", template: "<span id='GSchSummary_#=SchCycleId#_TueST'>#if(TueST !=null) { # #=formattedTime(TueST.Hours, TueST.Minutes)# #}#</span><span id='GSchSummary_#=SchCycleId#_TueET'># if(TueET !=null) { # - #=formattedTime(TueET.Hours, TueET.Minutes)# #}#</span>" },
            { field: "WedST", width: 90, title: "Wednesday", template: "<span id='GSchSummary_#=SchCycleId#_WedST'>#if(WedST !=null) { # #=formattedTime(WedST.Hours, WedST.Minutes)# #}#</span><span id='GSchSummary_#=SchCycleId#_WedET'># if(WedET !=null) { # - #=formattedTime(WedET.Hours, WedET.Minutes)# #}#</span>" },
            { field: "ThuST", width: 90, title: "Thursday", template: "<span id='GSchSummary_#=SchCycleId#_ThuST'>#if(ThuST !=null) { # #=formattedTime(ThuST.Hours, ThuST.Minutes)# #}#</span><span id='GSchSummary_#=SchCycleId#_ThuET'># if(ThuET !=null) { # - #=formattedTime(ThuET.Hours, ThuET.Minutes)# #}#</span>" },
            { field: "FriST", width: 90, title: "Friday", template: "<span id='GSchSummary_#=SchCycleId#_FriST'>#if(FriST !=null) { # #=formattedTime(FriST.Hours, FriST.Minutes)# #}#</span><span id='GSchSummary_#=SchCycleId#_FriET'>#if(FriET !=null) { # - #=formattedTime(FriET.Hours, FriET.Minutes)# #}#</span>" },
            { field: "SatST", width: 90, title: "Saturday", template: "<span id='GSchSummary_#=SchCycleId#_SatST'>#if(SatST !=null) { # #=formattedTime(SatST.Hours, SatST.Minutes)# #}#</span><span id='GSchSummary_#=SchCycleId#_SatET'># if(SatET !=null) { # - #=formattedTime(SatET.Hours, SatET.Minutes)# #}#</span>" }
            //{ field: "SunST", width: 80, title: "Sunday", template: "<span id='GSchSummary_#=SchCycleId#_SunST'>#if(SunST !=null) { # #=kendo.toString(SunST.Hours,'00')#:#=kendo.toString(SunST.Minutes,'00')# #}#</span><span id='GSchSummary_#=SchCycleId#_SunET'># if(SunET !=null) { # - #=kendo.toString(SunET.Hours,'00')#:#=kendo.toString(SunET.Minutes,'00')# #}#</span>" },
            //{ field: "MonST", width: 80, title: "Monday", template: "<span id='GSchSummary_#=SchCycleId#_MonST'>#if(MonST !=null) { # #=kendo.toString(MonST.Hours,'00')#:#=kendo.toString(MonST.Minutes,'00')# #}#</span><span id='GSchSummary_#=SchCycleId#_MonET'># if(MonET !=null) { # - #=kendo.toString(MonET.Hours,'00')#:#=kendo.toString(MonET.Minutes,'00')# #}#</span>" },
            //{ field: "TueST", width: 80, title: "Tuesday", template: "<span id='GSchSummary_#=SchCycleId#_TueST'>#if(TueST !=null) { # #=kendo.toString(TueST.Hours,'00')#:#=kendo.toString(TueST.Minutes,'00')# #}#</span><span id='GSchSummary_#=SchCycleId#_TueET'># if(TueET !=null) { # - #=kendo.toString(TueET.Hours,'00')#:#=kendo.toString(TueET.Minutes,'00')# #}#</span>" },
            //{ field: "WedST", width: 80, title: "Wednesday", template: "<span id='GSchSummary_#=SchCycleId#_WedST'>#if(WedST !=null) { # #=kendo.toString(WedST.Hours,'00')#:#=kendo.toString(WedST.Minutes,'00')##}#</span><span id='GSchSummary_#=SchCycleId#_WedET'># if(WedET !=null) { # - #=kendo.toString(WedET.Hours,'00')#:#=kendo.toString(WedET.Minutes,'00')# #}#</span>" },
            //{ field: "ThuST", width: 80, title: "Thursday", template: "<span id='GSchSummary_#=SchCycleId#_ThuST'>#if(ThuST !=null) { # #=kendo.toString(ThuST.Hours,'00')#:#=kendo.toString(ThuST.Minutes,'00')# #}#</span><span id='GSchSummary_#=SchCycleId#_ThuET'># if(ThuET !=null) { # - #=kendo.toString(ThuET.Hours,'00')#:#=kendo.toString(ThuET.Minutes,'00')# #}#</span>" },
            //{ field: "FriST", width: 80, title: "Friday", template: "<span id='GSchSummary_#=SchCycleId#_FriST'>#if(FriST !=null) { # #=kendo.toString(FriST.Hours,'00')#:#=kendo.toString(FriST.Minutes,'00')# #} #</span><span id='GSchSummary_#=SchCycleId#_FriET'>#if(FriET !=null) { # - #=kendo.toString(FriET.Hours,'00')#:#=kendo.toString(FriET.Minutes,'00')# #}#</span>" },
            //{ field: "SatST", width: 80, title: "Saturday", template: "<span id='GSchSummary_#=SchCycleId#_SatST'>#if(SatST !=null) { # #=kendo.toString(SatST.Hours,'00')#:#=kendo.toString(SatST.Minutes,'00')# #}#</span><span id='GSchSummary_#=SchCycleId#_SatET'># if(SatET !=null) { # - #=kendo.toString(SatET.Hours,'00')#:#=kendo.toString(SatET.Minutes,'00')# #}#</span>" }
        ]
    }).data("kendoGrid");

});

function formattedTime(hours, minutes) {
    if (hours != null && minutes != null) {

        dt = new Date(2012, 01, 01, hours, minutes, 0, 0);
        var hours = dt.getHours() == 0 ? "12" : dt.getHours() > 12 ? dt.getHours() - 12 : dt.getHours();
        //hours = (hours < 9 ? "0" : "") + hours;
        var minutes = (dt.getMinutes() < 10 ? "0" : "") + dt.getMinutes();
        var ampm = dt.getHours() < 12 ? "AM" : "PM";
        var formattedTime = hours + ":" + minutes + " " + ampm;
        return formattedTime;
    }
    else {
        return "";
    }
}

function string_of_enum(en, value) {
    for (var k in en) if (en[k] == value) return k;
    return null;
}

function string_of_Week(dayOfWeek) {
    var str = "";
    switch (dayOfWeek) {
        case DayofWeek.Sunday:
            str = "Sun";
            break;
        case DayofWeek.Monday:
            str = "Mon";
            break;
        case DayofWeek.Tuesday:
            str = "Tue";
            break;
        case DayofWeek.Wednesday:
            str = "Wed";
            break;
        case DayofWeek.Thrusday:
            str = "Thu";
            break;
        case DayofWeek.Friday:
            str = "Fri";
            break;
        case DayofWeek.Saturday:
            str = "Sat";
            break;
    }
    return str;
}

function startTime(value, rowIndex) {

    var startimepicker = $("#j_start_time").data("kendoTimePicker");
    startimepicker.value('');
    var dt = new Date();

    var hdGridDetailStart = '#SchSummary_' + rowIndex + '__' + string_of_Week(parseInt(value)) + 'ST';
    if ($(hdGridDetailStart).val() != undefined && $(hdGridDetailStart).val() != "") {
        var currentVals = $(hdGridDetailStart).val().split(':');
        dt = new Date(2012, 01, 01, parseInt(currentVals[0]), parseInt(currentVals[1]), 0, 0);
        startimepicker.value(dt);
    }
}

function endtime(value, rowIndex) {

    var endtimepicker = $("#j_end_time").data("kendoTimePicker");
    endtimepicker.value('');
    var dt = new Date();

    var hdGridDetailEnd = '#SchSummary_' + rowIndex + '__' + string_of_Week(parseInt(value)) + 'ET';
    if ($(hdGridDetailEnd).val() != undefined && $(hdGridDetailEnd).val() != "") {
        var currentVals = $(hdGridDetailEnd).val().split(':');
        dt = new Date(2012, 01, 01, parseInt(currentVals[0]), parseInt(currentVals[1]), 0, 0);
        endtimepicker.value(dt);
        _hasTimeAlreadyDefined = true;
    }
}

function saveTimeChangestoModel() {
    var schCycleRow = $("#hdCycleRow").val();
    var DayofWeek = $("#hdDayofWeek").val();
    var schCycleId = $("#hdCycleId").val();

    var startTime, endTime;
    var startHours = null, startMinutes = null, endHours = null, endMinutes = null;

    var startimepicker = $("#j_start_time").data("kendoTimePicker");
    var endtimepicker = $("#j_end_time").data("kendoTimePicker");

    if (startimepicker.value() != null && endtimepicker.value() != null) {
        startTime = new Date(startimepicker.value());
        endTime = new Date(endtimepicker.value());
        startHours = startTime.getHours();
        startMinutes = startTime.getMinutes();
        endHours = endTime.getHours();
        endMinutes = endTime.getMinutes();
    }
    else {
        //If the start and end time are not entered correct in the texbox
        if (($("#j_start_time").val() != "" && startimepicker.value() == null) || ($("#j_end_time").val() != "" && endtimepicker.value() == null)) {
            setStatus($('#schoperationStatus'), false, "Please enter valid Start/End times. Valid format - {HH:MMtt}");
            return false;
        }
    }
    setSchDetails(startHours, startMinutes, endHours, endMinutes, schCycleRow, DayofWeek, schCycleId);
    return true;
    //var form = $('#frmSchedule');
    //form.submit();
    //}
}

function formValidation(callback) {
    var isvalid = true;
    var text = $('#SchName').val().trim();
    if (text.trim() == "") {
        $("#formErr").html("Schedule Name is Required");
        isvalid = false;
    }
    else if (!/^[a-zA-Z]/.test(text)) {
        $("#formErr").html("Schedule Name is not valid. You can use letters, numbers and dashes and it must begin with a letter");
        isvalid = false;
    }
    else {
        if (/[\~\`\!\@@\#\$\%\^\&\*\_\+\=\{\}\|\\\/\[\]\:\;\'\"\<\,\>\.\?]/.test(text)) {
            $("#formErr").html("Schedule Name is not valid. You can use letters, numbers and dashes and it must begin with a letter");
            isvalid = false;
        }
    }
    if (/[\s]/.test(text)) {
        $("#formErr").html("No space is allowed in Schedule Name");
        isvalid = false;
    }
    var startDate, endDate;
    startDate = $('#j_start_date').val();
    endDate = $('#j_end_date').val();
    if (isvalid) {
        var sDate = new Date(startDate);
        var eDate = new Date(endDate);
        if (startDate != "" && endDate != "") {
            if ((startDate != "" && sDate == "Invalid Date") || (endDate != "" && eDate == "Invalid Date")) {
                $("#formErr").html("Please enter valid date(s)");
                isvalid = false;
            }
            else if (sDate > eDate) {
                $("#formErr").html("End date cannot be less than Start date");
                isvalid = false;
            }
            else if (+sDate === +eDate) {
                $("#formErr").html("Start and End date cannot be same.");
                isvalid = false;
            }
        }
    }
    callback(isvalid);
}

function onDataBound(e) {
    scrollToTop(this);
    var grid = $("#gdScheduleDetails").data("kendoGrid");
    var data = grid.dataSource;
    $(grid.tbody).on("click", "td", function (e) {
        var row = $(this).closest("tr");
        var colIdx = $("td", row).index(this);
        if (colIdx != 0) {
            var model = grid.dataItem(row);
            if (!e.ctrlKey && !e.altKey) {
                //alert(model.CycleName + '-' + colIdx);
                $("#hdCycleRow").val(row.index());
                $("#hdCycleId").val(model.SchCycleId);
                $("#hdDayofWeek").val(colIdx);
                _hasTimeAlreadyDefined = false;
                //assign start and endtime
                startTime(colIdx, row.index());
                endtime(colIdx, row.index());
                timeModified = false;
                setStatus($('#schoperationStatus'));
                var win = $("#wnSelectTime").data("kendoWindow");
                win.title("Select Time : " + string_of_enum(DayofWeek, colIdx) + " - " + model.CycleName);
                win.center().open();
            }
            else if (e.ctrlKey && !e.altKey) {
                copySchDetails(colIdx, row.index());
            }
            else if (e.altKey && !e.ctrlKey) {
                pasteSchDetails(colIdx, row.index(), model.SchCycleId);
            }
            else {
                setSchDetails(null, null, null, null, row.index(), colIdx, model.SchCycleId);
            }
        }
    });
}

function copySchDetails(day, rowIndex) {

    var hdGridDetailStart = '#SchSummary_' + rowIndex + '__' + string_of_Week(parseInt(day)) + 'ST';
    if ($(hdGridDetailStart).val() != undefined) {
        copiedStartTime = $(hdGridDetailStart).val();
    }
    var hdGridDetailEnd = '#SchSummary_' + rowIndex + '__' + string_of_Week(parseInt(day)) + 'ET';
    if ($(hdGridDetailEnd).val() != undefined) {
        copiedEndTime = $(hdGridDetailEnd).val();
    }
}
function pasteSchDetails(DayofWeek, rowIndex, schCycleId) {

    //If Something is copied
    if (copiedStartTime != "____" && copiedEndTime != "____") {
        var startHours = null, startMinutes = null, endHours = null, endMinutes = null;
        if (copiedStartTime != "" && copiedEndTime != "") {
            var startVals = copiedStartTime.split(':');
            var endVals = copiedEndTime.split(':');
            startHours = startVals[0];
            startMinutes = startVals[1];
            endHours = endVals[0];
            endMinutes = endVals[1];
        }
        setSchDetails(startHours, startMinutes, endHours, endMinutes, rowIndex, DayofWeek, schCycleId);
    }
}
function setDetailsareChanged() {
    $("#SaveSchedule").val(false);
    if ($("#SaveSchedule").val() == "false") {
        // $('#btnManageCycle').attr('disabled', 'disabled');
        //$('#divNotSavedMsg').show();
        $("#btnSaveSch").tooltip('show');
    }
    else {
        // $('#btnManageCycle').removeAttr('disabled');
        //$('#divNotSavedMsg').hide();
        $("#btnSaveSch").tooltip('hide');
    }
}

function setSchDetails(startHours, startMinutes, endHours, endMinutes, rowIndex, DayofWeek, schCycleId) {

    var hdSchDetailStart = '#SchSummary_' + rowIndex + '__' + string_of_Week(parseInt(DayofWeek)) + 'ST';
    var hdSchDetailEnd = '#SchSummary_' + rowIndex + '__' + string_of_Week(parseInt(DayofWeek)) + 'ET';

    var hdGridDetailStart = '#GSchSummary_' + schCycleId + '_' + string_of_Week(parseInt(DayofWeek)) + 'ST';
    var hdGridDetailEnd = '#GSchSummary_' + schCycleId + '_' + string_of_Week(parseInt(DayofWeek)) + 'ET';

    $("#HasSchDetailsChanged").val(true);

    if (startHours != null && startMinutes != null && endHours != null && endMinutes != null) {
        $(hdSchDetailStart).val(startHours + ":" + startMinutes + ":00");
        $(hdSchDetailEnd).val(endHours + ":" + endMinutes + ":00");
        //$(hdGridDetailStart).html("<i><b>" + ("0" + startHours).slice(-2) + ":" + ("0" + startMinutes).slice(-2) + "</b></i>");
        //$(hdGridDetailEnd).html("<i><b>" + " - " + ("0" + endHours).slice(-2) + ":" + ("0" + endMinutes).slice(-2) + "</b></i>");

        $(hdGridDetailStart).html("<i><b>" + formattedTime(startHours,startMinutes) + "</b></i>");
        $(hdGridDetailEnd).html("<i><b>" + " - " + formattedTime(endHours, endMinutes) + "</b></i>");
    }
    else {
        $(hdSchDetailStart).val("");
        $(hdSchDetailEnd).val("");
        $(hdGridDetailStart).html("");
        $(hdGridDetailEnd).html("");
    }

    setDetailsareChanged();
}