document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".no-collapse").forEach(link => {
        link.addEventListener("click", function (event) {
            event.stopPropagation(); // Prevents Bootstrap from hijacking the click
        });
    });

        // Prevent collapsing when clicking links inside the table
    document.querySelectorAll("a.no-collapse").forEach(link => {
        link.addEventListener("click", function (event) {
            event.stopPropagation(); // Stops Bootstrap collapse from triggering
        });
    });
});