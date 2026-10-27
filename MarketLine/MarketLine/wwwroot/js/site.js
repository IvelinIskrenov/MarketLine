(function () {
    function escapeHtml(value) {
        return String(value).replace(/[&<>"']/g, function (character) {
            return ({
                "&": "&amp;",
                "<": "&lt;",
                ">": "&gt;",
                '"': "&quot;",
                "'": "&#39;"
            })[character];
        });
    }

    function setupSuggest(input) {
        if (!input) {
            return;
        }

        var form = input.closest("form");
        if (!form) {
            return;
        }

        var box = document.createElement("div");
        box.className = "search-suggest";
        box.hidden = true;
        form.appendChild(box);
        input.setAttribute("autocomplete", "off");

        var timer = null;

        function hide() {
            box.hidden = true;
            box.innerHTML = "";
        }

        input.addEventListener("input", function () {
            clearTimeout(timer);
            var query = input.value.trim();
            if (query.length < 2) {
                hide();
                return;
            }

            timer = setTimeout(function () {
                fetch("/Item/Suggest?q=" + encodeURIComponent(query))
                    .then(function (response) {
                        if (!response.ok) {
                            throw new Error("suggest failed");
                        }
                        return response.json();
                    })
                    .then(function (items) {
                        if (!Array.isArray(items) || items.length === 0) {
                            hide();
                            return;
                        }

                        box.innerHTML = items.map(function (item) {
                            var brand = item.brand
                                ? "<small>" + escapeHtml(item.brand) + "</small>"
                                : "";
                            return '<a class="search-suggest-item" href="/Item/Details/' + item.id + '">' +
                                "<span>" + escapeHtml(item.name) + "</span>" +
                                brand +
                                "</a>";
                        }).join("");
                        box.hidden = false;
                    })
                    .catch(hide);
            }, 180);
        });

        document.addEventListener("click", function (event) {
            if (!form.contains(event.target)) {
                hide();
            }
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        document.querySelectorAll(".search-input").forEach(setupSuggest);
    });
})();
