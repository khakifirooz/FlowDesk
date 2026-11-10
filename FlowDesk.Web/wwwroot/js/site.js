(() => {
    "use strict";

    document.querySelectorAll("form[data-cascade]").forEach(form => {
        const project = form.querySelector("[data-project]");
        const task = form.querySelector("[data-task]");
        const member = form.querySelector("[data-member]");
        const save = form.querySelector("[data-save]");
        const message = form.querySelector("[data-cascade-status]");

        let request = null;
        let loading = false;

        const reset = (select, text) => {
            if (!select) {
                return;
            }

            const option = new Option(text, "", true, true);

            option.disabled = true;
            option.hidden = true;

            select.replaceChildren(option);
            select.disabled = true;
        };

        const update = () => {
            member.disabled =
                loading ||
                member.options.length <= 1 ||
                (!!task && !task.value);

            save.disabled =
                loading ||
                !project.value ||
                !member.value ||
                member.disabled ||
                (!!task && !task.value);
        };

        const updateLinks = () => {
            const selectors = [
                "[data-members-link]",
                "[data-create-task-link]"
            ];

            for (const selector of selectors) {
                const link = form.querySelector(selector);

                if (!link) {
                    continue;
                }

                link.hidden = !project.value;

                if (project.value) {
                    const url = new URL(link.href, location.href);

                    url.searchParams.set(
                        "projectId",
                        project.value
                    );

                    link.href = url.toString();
                }
            }
        };

        project.addEventListener("change", async () => {
            if (request) {
                request.abort();
            }

            const current = new AbortController();
            request = current;

            reset(member, "انتخاب مسئول");
            reset(task, "انتخاب تسک");

            updateLinks();

            loading = true;
            update();

            message.textContent = "در حال دریافت اطلاعات پروژه…";
            message.classList.remove("text-danger");

            try {
                const url = new URL(
                    form.dataset.optionsUrl,
                    location.href
                );

                url.searchParams.set(
                    "projectId",
                    project.value
                );

                url.searchParams.set(
                    "includeTasks",
                    task ? "true" : "false"
                );

                const response = await fetch(url, {
                    signal: current.signal,
                    headers: {
                        Accept: "application/json"
                    }
                });

                if (!response.ok) {
                    throw new Error("Could not load project options.");
                }

                const data = await response.json();

                if (request !== current) {
                    return;
                }

                for (const person of data.members) {
                    member.add(
                        new Option(person.name, person.id)
                    );
                }

                if (task) {
                    for (const item of data.tasks) {
                        const option = new Option(
                            item.title,
                            item.id
                        );

                        option.dataset.assigneeId =
                            item.assigneeId ?? "";

                        task.add(option);
                    }

                    task.disabled = data.tasks.length === 0;
                }

                if (data.members.length === 0) {
                    message.textContent =
                        "این پروژه عضوی ندارد؛ ابتدا عضو اضافه کنید.";
                } else if (task && data.tasks.length === 0) {
                    message.textContent =
                        "این پروژه تسکی ندارد؛ ابتدا تسک ایجاد کنید.";
                } else {
                    message.textContent =
                        "گزینه‌های مربوط به این پروژه آماده است.";
                }
            } catch (error) {
                if (error.name === "AbortError" ||
                    request !== current) {
                    return;
                }

                message.textContent =
                    "دریافت اطلاعات انجام نشد. پروژه را دوباره انتخاب کنید یا صفحه را تازه کنید.";

                message.classList.add("text-danger");
            } finally {
                if (request === current) {
                    loading = false;
                    update();
                }
            }
        });

        task?.addEventListener("change", () => {
            const previous =
                task.selectedOptions[0]?.dataset.assigneeId || "";

            const previousMemberExists = [...member.options].some(
                option => option.value === previous
            );

            member.value = previousMemberExists
                ? previous
                : "";

            update();
        });

        member.addEventListener("change", update);

        update();
        updateLinks();
    });

    document.addEventListener("submit", event => {
        const form = event.target;

        if (!(form instanceof HTMLFormElement)) {
            return;
        }

        if (event.defaultPrevented) {
            return;
        }

        if (window.jQuery &&
            jQuery.fn.valid &&
            !jQuery(form).valid()) {
            event.preventDefault();
            return;
        }

        if (form.dataset.confirm &&
            !window.confirm(form.dataset.confirm)) {
            event.preventDefault();
            return;
        }

        if (form.hasAttribute("data-prevent-double-submit")) {
            if (form.dataset.submitting === "true") {
                event.preventDefault();
                return;
            }

            form.dataset.submitting = "true";

            form.querySelectorAll('button[type="submit"]')
                .forEach(button => {
                    if (!button.disabled) {
                        button.dataset.wasEnabled = "true";
                        button.disabled = true;
                    }
                });
        }
    });

    window.addEventListener("pageshow", () => {
        document.querySelectorAll('form[data-submitting="true"]')
            .forEach(form => {
                delete form.dataset.submitting;

                form.querySelectorAll('[data-was-enabled="true"]')
                    .forEach(button => {
                        button.disabled = false;
                        delete button.dataset.wasEnabled;
                    });
            });
    });
})();