using System.Collections.Generic;
using Campus_SMS.Models;

namespace Campus_SMS.ViewModels
{
    public record AdminDashboardViewModel(Faculty SelectedFaculty, Student SelectedStudent, Course SelectedCourse)
    {
        // Faculty Section
        public List<Faculty> FacultyList { get; set; } = new List<Faculty>();

        public AdminDashboardViewModel(Faculty selectedFaculty) : this(selectedFaculty ?? throw new ArgumentNullException(nameof(selectedFaculty)), default, default) => SelectedFaculty = selectedFaculty ?? throw new ArgumentNullException(nameof(selectedFaculty));

        public AdminDashboardViewModel() : this(default, default, default)
        {
        }

        public AdminDashboardViewModel(Faculty selectedFaculty, Student selectedStudent, Course selectedCourse, List<Faculty> facultyList, List<Student> students, List<ChatMessage> chatMessages, List<Course> classes, List<FileData> courseFiles) : this(selectedFaculty, selectedStudent, selectedCourse)
        {
            if (selectedFaculty is null)
            {
                throw new ArgumentNullException(nameof(selectedFaculty));
            }

            if (selectedStudent is null)
            {
                throw new ArgumentNullException(nameof(selectedStudent));
            }

            if (selectedCourse is null)
            {
                throw new ArgumentNullException(nameof(selectedCourse));
            }

            FacultyList = facultyList ?? throw new ArgumentNullException(nameof(facultyList));
            Students = students ?? throw new ArgumentNullException(nameof(students));
            ChatMessages = chatMessages ?? throw new ArgumentNullException(nameof(chatMessages));
            Classes = classes ?? throw new ArgumentNullException(nameof(classes));
            CourseFiles = courseFiles ?? throw new ArgumentNullException(nameof(courseFiles));
        }

        // Students Section
        public List<Student> Students { get; set; } = new List<Student>();
        public List<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

        // Course Management Section
        public List<Course> Classes { get; set; } = new List<Course>();
        public List<FileData> CourseFiles { get; set; } = new List<FileData>();
    }
}