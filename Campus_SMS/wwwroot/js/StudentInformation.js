document.addEventListener("DOMContentLoaded", function () {
    const studentSelect = document.getElementById("studentSelect");
    const studentInfoDiv = document.getElementById("studentInfo");
    const studentCourseUl = document.getElementById("enrolledCourse")

    if (studentSelect) {
        $('#studentSelect').on('change', function () {
            const smsUserId = this.value;

            if (smsUserId) {
                fetch(`/Admin/GetSMSInfo/${smsUserId}`)
                    .then(response => response.json())
                    .then(data => {
                        if (data.error) {
                            studentInfoDiv.innerHTML = `<p>${data.error}</p>`;
                            return;
                        }

                        let htmlStudent = `
                            <p><strong>Phone:</strong> ${data.phoneNumber}</p>
                        `;

                        let htmlEnrolled = "";
                        if (data.enrolledCourses.length > 0) {
                            data.enrolledCourses.forEach(course => {
                                htmlEnrolled += `<li>${course}</li>`;
                            });
                        } else {
                            htmlEnrolled = "<li>No classes are enrolled</li>"
                        }
                        

                        studentInfoDiv.innerHTML = htmlStudent;
                        studentCourseUl.innerHTML = htmlEnrolled;
                    });

            } else {
                studentInfoDiv.innerHTML = "";
                studentCourseUl.innerHTML = "";
            }
        });
    }
});