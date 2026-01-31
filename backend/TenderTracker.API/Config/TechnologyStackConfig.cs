using System.Text.Json;

namespace TenderTracker.API.Config
{
    public class TechnologyStackConfig
    {
        public List<Technology> Technologies { get; set; } = new List<Technology>();
        public AnalysisSettings Settings { get; set; } = new AnalysisSettings();
    }

    public class Technology
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Aliases { get; set; } = new List<string>();
        public int Weight { get; set; } = 1; // Вес технологии при анализе
    }

    public class AnalysisSettings
    {
        public int MinimumMatchScore { get; set; } = 60; // Минимальный процент совпадения для совместимости
        public bool EnableAutoAnalysis { get; set; } = true;
        public bool RequireManualVerification { get; set; } = false;
        public int MaxTextLength { get; set; } = 10000; // Максимальная длина текста для анализа
    }

    public static class DefaultTechnologyStack
    {
        public static TechnologyStackConfig GetDefault()
        {
            return new TechnologyStackConfig
            {
                Technologies = new List<Technology>
                {
                    new Technology { 
                        Name = ".NET", 
                        Aliases = new List<string> { ".NET", "C#", "CSharp", "ASP.NET", ".NET Core", "Entity Framework", "EF Core", "ASP.NET Core", "Blazor", "MVC" },
                        Weight = 2 
                    },
                    new Technology { 
                        Name = "PostgreSQL", 
                        Aliases = new List<string> { "PostgreSQL", "Postgres", "PSQL", "PostgreSQL Database", "pg" },
                        Weight = 1 
                    },
                    new Technology { 
                        Name = "React", 
                        Aliases = new List<string> { "React", "React.js", "ReactJS", "Redux", "React Native", "Next.js", "Gatsby" },
                        Weight = 2 
                    },
                    new Technology { 
                        Name = "Java", 
                        Aliases = new List<string> { "Java", "Spring", "Spring Boot", "J2EE", "Java EE", "Jakarta EE", "Hibernate", "Maven", "Gradle" },
                        Weight = 2 
                    },
                    new Technology { 
                        Name = "Angular", 
                        Aliases = new List<string> { "Angular", "Angular.js", "AngularJS", "TypeScript", "RxJS", "Angular Material" },
                        Weight = 2 
                    },
                    new Technology { 
                        Name = "Android", 
                        Aliases = new List<string> { "Android", "Kotlin", "Android SDK", "Android Studio", "Jetpack", "Compose", "Flutter" },
                        Weight = 1 
                    },
                    new Technology { 
                        Name = "DevOps", 
                        Aliases = new List<string> { "Docker", "Kubernetes", "CI/CD", "Jenkins", "GitLab", "Azure DevOps", "GitHub Actions", "Terraform", "Ansible", "AWS", "Azure", "GCP" },
                        Weight = 1 
                    },
                    new Technology { 
                        Name = "ML", 
                        Aliases = new List<string> { "Machine Learning", "ML", "TensorFlow", "PyTorch", "scikit-learn", "Keras", "OpenCV", "Pandas", "NumPy" },
                        Weight = 1 
                    },
                    new Technology { 
                        Name = "RAG ML", 
                        Aliases = new List<string> { "RAG", "Retrieval-Augmented Generation", "LLM", "LangChain", "OpenAI", "GPT", "BERT", "Transformers", "Vector Database" },
                        Weight = 1 
                    }
                },
                Settings = new AnalysisSettings
                {
                    MinimumMatchScore = 60,
                    EnableAutoAnalysis = true,
                    RequireManualVerification = false,
                    MaxTextLength = 10000
                }
            };
        }
    }
}
