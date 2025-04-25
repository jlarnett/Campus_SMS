document.addEventListener("DOMContentLoaded", function () {
    const facultySelect = document.getElementById("facultySelect");
    const facultyInfoDiv = document.getElementById("facultyInfo");
    const facultyCourseUl = document.getElementById("facultyCourse");

    if (facultySelect) {
        $('#facultySelect').on('change', function () {
            const facultyId = this.value;

            if (facultyId) {
                fetch(`/Admin/GetFacultyInfo/${facultyId}`)
                    .then(response => response.json())
                    .then(data => {

                        if (data.error) {
                            facultyInfoDiv.innerHTML = `<p>${data.error}</p>`;
                            facultyCourseUl.innerHTML = "";
                            return;
                        }

                        let htmlFaculty = `
                            <h2>${data.email}</h2>
                        `;

                        let htmlCourse = "";

                        data.classesList.forEach(c => {
                            htmlCourse += `
                                <li>
                                    (${c.usiCode}) ${c.className}
                                    <button class="remove-btn" data-classid="${c.classId}">Remove</button>
                                </li>
                            `;
                        });

                        facultyInfoDiv.innerHTML = htmlFaculty;
                        facultyCourseUl.innerHTML = htmlCourse;

                        document.querySelectorAll('.remove-btn').forEach(button => {
                            button.addEventListener('click', function () {
                                const courseId = this.dataset.classid;
                                const facultyId = document.getElementById("facultySelect").value;

                                if (!courseId || !facultyId) {
                                    alert("Something went wrong — missing data.");
                                    return;
                                }

                                if (!confirm(`Remove course ${courseId} from this faculty member?`)) return;

                                fetch('/Admin/RemoveFacultyCourse', {
                                    method: 'POST',
                                    headers: {
                                        'Content-Type': 'application/json'
                                    },
                                    body: JSON.stringify({
                                        facultyId: facultyId,
                                        courseId: parseInt(courseId)
                                    })
                                })
                                    .then(res => res.json())
                                    .then(result => {
                                        if (result.success) {
                                            document.getElementById("facultySelect").dispatchEvent(new Event("change"));
                                        } else {
                                            alert("Error: " + result.message);
                                        }
                                    })
                                    .catch(err => {
                                        console.error("Remove fetch error:", err);
                                    });
                            });
                        });
                    });
            } else {
                facultyInfoDiv.innerHTML = "";
                facultyCourseUl.innerHTML = "";
            }
        });
    }
});

