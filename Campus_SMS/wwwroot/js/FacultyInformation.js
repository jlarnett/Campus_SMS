document.addEventListener("DOMContentLoaded", function ()
{
    const facultySelect = document.getElementById("facultySelect");
    const facultyInfoDiv = document.getElementById("facultyInfo");
    const facultyCourseUl = document.getElementById("facultyCourse")

    if (facultySelect)
    {
        $('#facultySelect').on('change', function ()
        {
            const facultyId = this.value;

            if (facultyId)
            {
                fetch(`/Admin/GetFacultyInfo/${facultyId}`)
                    .then(response => response.json())
                    .then(data =>
                    {
                        if (data.error)
                        {
                            facultyInfoDiv.innerHTML = `<p>${data.error}</p>`;
                            facultyCourseUl.innerHTML = "";
                            return;
                        }

                        let htmlFaculty = `
                            <h2>${data.firstName} ${data.lastName}</h2>
                            <p><strong>Email:</strong> ${data.email}</p>
                        `;

                        let htmlCourse = ""; 
                        data.classesList.forEach(c =>
                        {
                            htmlCourse += `<li>(${c.usiCode}) ${c.className}</li>`;
                        })

                        facultyInfoDiv.innerHTML = htmlFaculty;
                        facultyCourseUl.innerHTML = htmlCourse;
                    });
                
            } else {
                facultyInfoDiv.innerHTML = "";
                facultyCourseUl.innerHTML = "";
            }
        });
    }
});
