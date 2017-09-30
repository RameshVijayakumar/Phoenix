var t;
var t1;
var _timeInterval = 1000;
var _notifyUserPopupTimeInterval = 60000;
var _valDetermineTimeOut = -60000;
$(function () {
    $.ajaxSetup({
        cache: false
    });
    // intialize timeout for any ajax call - autoredirect 
    $(document).ajaxStart(function () {

        CheckSessionTimeoutonAjaxStart(function(data)
        {
            //If Session is present then reset everything
            if(data)
            {
                localStorage.setItem('NewActivity', true);
                clearTimeout(t1);
                //ResetSessionTimeOutValue();
                clearTimeout(t);
                DisplaySessionTimeout();
            }
        });

        //Diable all functional Buttons on ajax Call
        $(".functionalbtn").addClass('disabledAnchor');
        $(".functionalbtn").attr('disabled', 'disabled');

        $(".savefunctionalbtn").addClass('disabledAnchor');
        $(".savefunctionalbtn").button();
        //$(".savefunctionalbtn").button('loading');

        //$(".addfunctionalbtn").addClass('disabledAnchor');
        //$(".addfunctionalbtn").button();
        //$(".addfunctionalbtn").button('loading');
        //$(".actionInprogress").css("visibility", "visible");
        showWaitSpinner();
    })
    $(document).ajaxStop(function () {

        $(".functionalbtn").removeClass('disabledAnchor');
        $(".functionalbtn").removeAttr('disabled', 'disabled');

        $(".savefunctionalbtn").removeClass('disabledAnchor');
        $(".savefunctionalbtn").button();
        //$(".savefunctionalbtn").button('reset');

        //$(".addfunctionalbtn").removeClass('disabledAnchor');
        //$(".addfunctionalbtn").button();
        //$(".addfunctionalbtn").button('reset');
        //$(".actionInprogress").css("visibility", "hidden");
        hideWaitSpinner();
    })
});

// autoredirect 
$(document).ready(function () {
    // Start Timer if is not Login Page
    if (isUserAuthenticated == 'True') {
        localStorage.setItem('NewActivity', true);
        clearTimeout(t1);
        //ResetSessionTimeOutValue();
        DisplaySessionTimeout();
        KeepAlive();
    }
});

$(document).bind("keyup", function (e) {
    if (e.keyCode == kendo.keys.ESC) {
        var visibleWindow = $(".k-window:visible > .k-window-content");
        if (visibleWindow.length) {
            if (visibleWindow.find("k-cancel")) {
                //nothing                
            }
            else {
                visibleWindow.data("kendoWindow").close();
            }
        }
    }
});
function DisplaySessionTimeout() {

    // Reset the timer if is a new activity
    var isNewActivity = localStorage.getItem('NewActivity');
    if (isNewActivity == "true") {
        ResetSessionTimeOutValue();
        t1 = setTimeout("RemoveNewActivityFlag()", _timeInterval);
    }

    //assigning minutes left to session timeout to Label
    var sessionTimeout = $("#hdnSessionTimeOut").val();

    //Notify user before one minute
    if (sessionTimeout == _notifyUserPopupTimeInterval) {
        NotifyUser();
    }

    sessionTimeout = sessionTimeout - _timeInterval;
    //alert(sessionTimeout);

    //if session is not less than 0
    if (sessionTimeout >= 0) {
        //call the function again after delay
        $("#hdnSessionTimeOut").val(sessionTimeout);
        t = setTimeout("DisplaySessionTimeout()", _timeInterval);
    }
    else {
        //redirect
        logoutTheUser();
    }
}

function CheckSessionTimeoutonAjaxStart(callback) {
    var sessionTimeout = $("#hdnSessionTimeOut").val();

    //if session is less than 0
    if (sessionTimeout < 0) 
    {
        logoutTheUser();
        callback(false);
    }
    else
    {
        callback(true);
    }
}
function RemoveNewActivityFlag() {
    localStorage.setItem('NewActivity', false);
    clearTimeout(t1);
}
function logoutTheUser() {
    $("#hdnSessionTimeOut").val(_valDetermineTimeOut);
    //localStorage.setItem('SessionTimeOutValue', -60000);
    if (document.getElementById('logoutForm') != null) {
        document.getElementById('logoutForm').submit();
    }
    else {
        clearTimeout(t);
    }
}
function NotifyUser() {

    confirmWindow("Confirm", "Current login session is going to expire in <span id='timer_div'>60</span> seconds. Click 'Stay Logged-in' to continue working or 'Logout' to exit.", function () { }, "400px", "Stay Logged-in", "Logout", function (data) {
        if (data == true) {
            //Reset the Session
            KeepAlive();
            ResetSessionTimeOutValue();
        } else {
            logoutTheUser();
        }
        clearInterval(t2);
        clearTimeout(t3);
    });

    var seconds_left = _notifyUserPopupTimeInterval/1000;
    var t2 = setInterval(function () {
        seconds_left = seconds_left - 10;
        if (seconds_left <= 0) {
            clearInterval(t2);
        }
        document.getElementById('timer_div').innerHTML = seconds_left;
    }, 10000);

    var t3 = setTimeout(function () {
        $("#confirmWindow").data("kendoWindow").close();
    }, _notifyUserPopupTimeInterval);
}
function KeepAlive() {
    // ajax call to keep the server-side session alive
    $.ajax({
        url: '/Account/KeepSessionAlive',
        type: 'GET',
        dataType: 'json',
        async:false
    });
}