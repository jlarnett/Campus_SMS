$(document).ready(function () {
    const studentInfoDiv = document.getElementById("studentInfo");
    const studentCourseUl = document.getElementById("enrolledCourse");

    $('#studentSelect').on('change', function () {
        const smsUserId = $(this).val();

        if (!smsUserId) {
            studentInfoDiv.innerHTML = "";
            studentCourseUl.innerHTML = "";
            return;
        }

        fetch(`/Admin/GetSMSInfo/${smsUserId}`)
            .then(response => response.json())
            .then(data => {

                if (data.error) {
                    studentInfoDiv.innerHTML = `<p>${data.error}</p>`;
                    studentCourseUl.innerHTML = "";
                    return;
                }

                studentInfoDiv.innerHTML = `
                    <p><strong>Phone:</strong> ${data.phoneNumber}</p>
                `;

                let htmlEnrolled = "";
                if (data.enrolledCourses && data.enrolledCourses.length > 0) {
                    data.enrolledCourses.forEach(course => {
                        const cleanCourse = String(course).trim();
                        htmlEnrolled += `
                            <li>
                                ${cleanCourse}
                                <button class="remove-btn" data-course="${cleanCourse}">Remove</button>
                            </li>
                        `;
                    });
                } else {
                    htmlEnrolled = "<li>No classes are enrolled</li>";
                }

                studentCourseUl.innerHTML = htmlEnrolled;

                document.querySelectorAll('.remove-btn').forEach(button => {
                    button.addEventListener('click', function () {
                        const courseToRemove = this.dataset.course;

                        if (!confirm(`Remove course "${courseToRemove}"?`)) return;

                        fetch('/Admin/RemoveEnrolledCourse', {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/json'
                            },
                            body: JSON.stringify({
                                userId: smsUserId,
                                course: courseToRemove
                            })
                        })
                            .then(res => res.json())
                            .then(result => {
                                if (result.success) {
                                    $('#studentSelect').trigger('change');
                                } else {
                                    alert("Error: " + result.message);
                                }
                            });
                    });
                });
            })
           
    });
});
