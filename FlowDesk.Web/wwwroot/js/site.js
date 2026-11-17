(() => {
    "use strict";

    document.querySelectorAll("form[data-cascade]").forEach(form => {
        const project = form.querySelector("[data-project]");
        const task = form.querySelector("[data-task]");
        const member = form.querySelector("[data-member]");
        const save = form.querySelector("[data-save]");
        const message = form.querySelector("[data-cascade-status]");

        if (!project || !member || !save || !message) return;

        let request = null;
        let loading = false;

        const hasMembers = () => [...member.options].some(
            option => option.value !== "" && !option.disabled
        );

        const restoreValue = (select, value) => {
            if (!select) return;

            const exists = [...select.options].some(
                option => option.value === value && !option.disabled
            );

            select.value = exists ? value : "";
        };

        const reset = (select, text) => {
            if (!select) return;

            const option = new Option(text, "", true, true);

            option.disabled = true;
            option.hidden = true;

            select.replaceChildren(option);
            select.disabled = true;
        };

        const update = () => {
            member.disabled =
                loading ||
                !project.value ||
                !hasMembers() ||
                (!!task && !task.value);

            save.disabled =
                loading ||
                !project.value ||
                !member.value ||
                member.disabled ||
                (!!task && !task.value);
        };

        const updateLinks = () => {
            for (const selector of [
                "[data-members-link]",
                "[data-create-task-link]"
            ]) {
                const link = form.querySelector(selector);

                if (!link) continue;

                link.hidden = !project.value;

                if (project.value) {
                    const url = new URL(link.href, location.href);

                    url.searchParams.set("projectId", project.value);

                    link.href = url.toString();
                }
            }
        };

        const loadProjectOptions = async (preserveSelection = false) => {
            const previousTask =
                preserveSelection && task ? task.value : "";

            const previousMember =
                preserveSelection ? member.value : "";

            request?.abort();

            const current = new AbortController();
            request = current;

            reset(member, "انتخاب مسئول");
            reset(task, "انتخاب تسک");

            updateLinks();
            message.classList.remove("text-danger");

            if (!project.value) {
                loading = false;
                message.textContent = "ابتدا یک پروژه انتخاب کنید.";
                update();
                return;
            }

            loading = true;
            update();

            message.textContent = "در حال دریافت اطلاعات پروژه…";

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
                    cache: "no-store",
                    headers: {
                        Accept: "application/json"
                    }
                });

                if (!response.ok) {
                    throw new Error("Could not load project options.");
                }

                const data = await response.json();

                if (request !== current) return;

                if (!Array.isArray(data.members) ||
                    (task && !Array.isArray(data.tasks))) {
                    throw new Error("Invalid project options response.");
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

                if (preserveSelection) {
                    restoreValue(task, previousTask);

                    const savedAssignee =
                        task?.selectedOptions[0]
                            ?.dataset.assigneeId || "";

                    restoreValue(
                        member,
                        previousMember || savedAssignee
                    );
                }

                message.textContent = data.members.length === 0
                    ? "این پروژه عضوی ندارد؛ ابتدا عضو اضافه کنید."
                    : task && data.tasks.length === 0
                        ? "این پروژه تسکی ندارد؛ ابتدا تسک ایجاد کنید."
                        : "مسئول را از اعضای همین پروژه انتخاب کنید.";
            } catch (error) {
                if (error.name === "AbortError" ||
                    request !== current) {
                    return;
                }

                reset(member, "انتخاب مسئول");
                reset(task, "انتخاب تسک");

                message.textContent =
                    "دریافت اطلاعات انجام نشد. صفحه را تازه کنید و دوباره تلاش کنید.";

                message.classList.add("text-danger");
            } finally {
                if (request === current) {
                    loading = false;
                    update();
                }
            }
        };

        project.addEventListener("change", () => {
            void loadProjectOptions();
        });

        task?.addEventListener("change", () => {
            const assigneeId =
                task.selectedOptions[0]?.dataset.assigneeId || "";

            restoreValue(member, assigneeId);
            update();
        });

        member.addEventListener("change", update);

        update();
        updateLinks();

        // A project may already be selected before any change event occurs.
        if (project.value && !hasMembers()) {
            void loadProjectOptions(true);
        }
    });

    document.addEventListener("submit", event => {
        const form = event.target;

        if (!(form instanceof HTMLFormElement) ||
            event.defaultPrevented) {
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