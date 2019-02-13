// Shared _Layout.cshtml -------------

// Mobile Menu Toggle
$("#menu-toggle").click(function (e) {
    e.preventDefault();
    $("#wrapper").toggleClass("active");
});

// Determine which menu item should be set to 'Active' based on URL
$(function () {

    var rootPath = location.pathname.split('/')[1];
    rootPath = rootPath.toLowerCase();
    activeMenuItem = "";
    if (rootPath == "") {
        activeMenuItem = "dashboard";
    }
    else {
        activeMenuItem = rootPath;
    }

    $("#" + activeMenuItem + "-nav").addClass('active');
});