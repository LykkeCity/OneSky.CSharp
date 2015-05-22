﻿namespace OneSkyDotNetTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;

    using FluentAssertions;

    using Xunit;

    public class Platform
    {
        private static Random random = new Random((int)DateTime.Now.Ticks);//thanks to McAden
        private string RandomString(int size)
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }

        private OneSkyDotNet.Json.IPlatform platform =
            OneSkyDotNet.Json.OneSkyClient.CreateClient(Settings.PublicKey, Settings.PrivateKey).Platform;

        private string projectGroupLocale = "en";

        private string fileNameDe = "File.de.txt";
        private string fileNameEn = "File.en.txt";

        private string filePathEn
        {
            get
            {
                return string.Format("Data\\{0}", this.fileNameDe);
            }
        }

        private string filePathBe
        {
            get
            {
                return string.Format("Data\\{0}", this.fileNameEn);
            }
        }

        private string projectGroupName;

        private int projectGroupId;

        private int projectGroupId2;

        private int projectId;

        private string projectName;

        private int fileImportIdA;

        private int fileImportIdB;

        [Fact]
        public void GrandTest()
        {
            this.Nuke();
            
            this.ProjectGroupCreate();
            this.ProjectGroupCreateFake();
            this.ProjectGroupList();
            this.ProjectGroupListMeta();
            this.ProjectGroupShowFresh();
            this.ProjectGroupLanguagesFresh();

            this.ProjectCreate();
            this.ProjectListFresh();
            this.ProjectUpdate();
            this.ProjectShowFresh();
            this.ProjectLanguageFresh();

            this.FileUploadBaseLanguage();
            this.FileUploadNonBaseLanguage();
            this.ProjectLanguage();
            this.FileList();

            this.QuotationShow();

            // Cleanup
            this.FileDelete();
            this.ProjectDelete();
            this.ProjectGroupDeleteFake();
            this.ProjectGroupDelete();
        }

        public void ProjectGroupCreate()
        {
            this.projectGroupName = this.RandomString(8);

            var response = this.platform.ProjectGroup.Create(this.projectGroupName, this.projectGroupLocale);

            response.MetaContent.Status.Should().Be(201, "[Documentation]").And.Be(response.StatusCode);

            response.DataContent.Name.Should().StartWith(this.projectGroupName, "[Created that way]");
            response.DataContent.BaseLanguage.Should().NotBeNull();
            response.DataContent.BaseLanguage.Locale.Should().Be(this.projectGroupLocale, "[Created that way]");

            this.projectGroupId = response.DataContent.Id;
        }

        public void ProjectGroupCreateFake()
        {
            var response = this.platform.ProjectGroup.Create(this.RandomString(4));

            response.MetaContent.Status.Should().Be(201, "[Documentation]").And.Be(response.StatusCode);
            
            response.DataContent.BaseLanguage.Should().NotBeNull();
            response.DataContent.BaseLanguage.Locale.Should().Be("en", "[As Default]");

            this.projectGroupId2 = response.DataContent.Id;
        }

        public void ProjectGroupList()
        {
            var perPage = 100;
            var response = this.platform.ProjectGroup.List(1, perPage);

            response.MetaContent.Status.Should().Be(200, "[Documentation]").And.Be(response.StatusCode);

            response.DataContent.Should()
                .NotBeNullOrEmpty("We've created 2 Project Groups by now")
                .And.HaveCount(response.MetaContent.RecordCount)
                .And.Contain(x => x.Id == this.projectGroupId2, "we created 'fake' project group")
                .And.Contain(x => x.Name.StartsWith(this.projectGroupName));
        }

        public void ProjectGroupListMeta()
        {
            var perPage = 1;
            var response1 = this.platform.ProjectGroup.List(1, perPage);
            var response2 = this.platform.ProjectGroup.List(2, perPage);

            response1.MetaContent.FirstPage.Should()
                .NotBeNullOrWhiteSpace("First page always exists")
                .And.Be(response2.MetaContent.FirstPage);

            response2.MetaContent.LastPage.Should()
                .NotBeNullOrWhiteSpace("Last page always exists")
                .And.Be(response1.MetaContent.LastPage)
                .And.NotBe(
                    response1.MetaContent.FirstPage,
                    "We created 2 Project Groups but displaying 1 per page. There should definitrly be more that one page");

            response1.MetaContent.NextPage.Should().NotBeNullOrWhiteSpace();
            response2.MetaContent.PreviousPage.Should().NotBeNullOrWhiteSpace();
        }

        public void ProjectGroupShowFresh()
        {
            var response = this.platform.ProjectGroup.Show(this.projectGroupId);

            response.MetaContent.Status.Should().Be(200, "[Documentation]").And.Be(response.StatusCode);

            response.DataContent.Name.Should().StartWith(this.projectGroupName);
            response.DataContent.EnabledLanguageCount.Should().Be(1, "Only language set during creation");
            response.DataContent.ProjectCount.Should().Be(0, "We have not created projects yet");
        }

        public void ProjectGroupLanguagesFresh()
        {
            var response = this.platform.ProjectGroup.Languages(this.projectGroupId);

            response.MetaContent.Status.Should().Be(200, "[Documentation]").And.Be(response.StatusCode);

            response.DataContent.Should()
                .HaveCount(response.MetaContent.RecordCount)
                .And.HaveCount(1, "Clean start - only one language")
                .And.Contain(x => x.Locale == this.projectGroupLocale, "We created this locale")
                .And.ContainSingle(x => x.IsBaseLanguage, "One base language");
        }

        public void ProjectCreate() 
        {
            this.projectName = RandomString(8);

            var projectType = this.platform.ProjectType.List().DataContent.First(x => x.Code.EndsWith("-others"));

            var response = this.platform.Project.Create(this.projectGroupId, projectType.Code, this.projectName);

            response.StatusCode.Should().Be(201);

            response.DataContent.Name.Should().Be(this.projectName);
            response.DataContent.ProjectType.Name.Should().Be(projectType.Name);            
            response.DataContent.ProjectType.Code.Should().Be(projectType.Code);
            response.DataContent.Description.Should().BeNullOrWhiteSpace("Created without description");

            this.projectId = response.DataContent.Id;
        }

        public void ProjectListFresh() 
        {
            var response = this.platform.Project.List(this.projectGroupId);

            response.DataContent.Should().HaveCount(response.MetaContent.RecordCount)
                .And.Contain(x => x.Id == this.projectId)
                .And.Contain(x => x.Name == this.projectName);
        }

        public void ProjectUpdate() 
        {
            var response = this.platform.Project.Update(this.projectId, this.projectName, "TestDesc");
            response.StatusCode.Should().Be(200);                        
        }

        public void ProjectShowFresh() 
        {
            var response = this.platform.Project.Show(this.projectId);

            response.DataContent.Name.Should().Be(this.projectName);
            response.DataContent.Description.Should().Be("TestDesc");
            response.DataContent.ProjectType.Code.Should().EndWith("-others");
        }

        public void ProjectLanguageFresh() 
        {
            var response = this.platform.Project.Languages(this.projectId);

            response.DataContent.Should()
                .HaveCount(response.MetaContent.RecordCount)
                .And.HaveCount(1, "Clean start - only one language")
                .And.Contain(x => x.Locale == this.projectGroupLocale, "We created this locale")
                .And.ContainSingle(x => x.IsBaseLanguage, "One base language");
        }

        public void FileUploadBaseLanguage()
        {
            var response = this.platform.File.Upload(this.projectId, this.filePathBe, "INI");
            response.MetaContent.Status.Should().Be(201);
            response.DataContent.Name.Should().Be(this.fileNameEn, "as uploaded");
            response.DataContent.Locale.Locale.Should().Be(this.projectGroupLocale, "as base lunguage");
            response.DataContent.Format.Should().Be("INI");
            this.fileImportIdA = response.DataContent.Import.Id;
        }

        public void FileUploadNonBaseLanguage()
        {
            var response = this.platform.File.Upload(this.projectId, this.filePathEn, "INI", "de");
            response.MetaContent.Status.Should().Be(201);
            response.DataContent.Name.Should().Be(this.fileNameDe, "as uploaded");
            response.DataContent.Locale.Locale.Should().Be("de", "as specified");
            response.DataContent.Format.Should().Be("INI");
            this.fileImportIdB = response.DataContent.Import.Id;
        }

        public void ProjectLanguage()
        {
            var response = this.platform.Project.Languages(this.projectId);

            response.DataContent.Should()
                .HaveCount(response.MetaContent.RecordCount)
                .And.HaveCount(2, "Only one non-base language added")
                .And.Contain(x => x.Locale == "de", "added one")
                .And.ContainSingle(x => x.IsBaseLanguage, "Still only one base language");
        }
        public void FileList()
        {
            // Sleeping for 10 seconds. Just to be sure that file import is done.
            Thread.Sleep(TimeSpan.FromSeconds(10));

            var response = this.platform.File.List(this.projectId);

            response.MetaContent.Status.Should().Be(200);
            response.DataContent.Should()
                .HaveCount(response.MetaContent.RecordCount)
                .And.Contain(x => x.Name == this.fileNameEn, "We have created one")
                .And.NotContain(
                    x => x.Name == this.fileNameDe,
                    "this file contains same keys, so it SHOULD be added as translation, not as new file")
                .And.Contain(x => x.LastImport != null && x.LastImport.Id == this.fileImportIdA, "As one of imported files");
        }

        public void QuotationShow()
        {
            var responseDe = this.platform.Quotation.Show(this.projectId, new List<string> { this.fileNameEn }, "de");
            var responseFr = this.platform.Quotation.Show(this.projectId, new List<string> { this.fileNameEn }, "fr");

            responseDe.DataContent.Files.Should().Contain(x => x.Name == this.fileNameEn);
            responseDe.DataContent.FromLanguage.Locale.Should().Be(this.projectGroupLocale);
            responseFr.DataContent.FromLanguage.Locale.Should().Be(this.projectGroupLocale);
            responseDe.DataContent.ToLanguage.Locale.Should().Be("de");
            responseFr.DataContent.ToLanguage.Locale.Should().Be("fr");
            responseFr.DataContent.TranslationAndReview.TotalCost.Should()
                .BeGreaterThan(responseDe.DataContent.TranslationAndReview.TotalCost);
        }

        // Cleaning up
        public void FileDelete()
        {
            var response = this.platform.File.Delete(this.projectId, this.fileNameEn);
            response.MetaContent.Status.Should().Be(200);
            response.DataContent.Name.Should().StartWith(this.fileNameEn);
        }
        
        public void ProjectDelete()
        {
            var response = this.platform.Project.Delete(this.projectId);
            response.StatusCode.Should().Be(200);
        }

        public void ProjectGroupDeleteFake()
        {
            var response = this.platform.ProjectGroup.Delete(this.projectGroupId2);
            response.StatusCode.Should().Be(200);
        }

        public void ProjectGroupDelete()
        {
            var response = this.platform.ProjectGroup.Delete(this.projectGroupId);
            response.StatusCode.Should().Be(200);
        }

        public void Nuke()
        {
            // WARNING! This will remove everi bit from your account
            var ids = this.platform.ProjectGroup.List(1, 100).DataContent.Select(x => x.Id);
            foreach (var id in ids)
            {
                this.platform.ProjectGroup.Delete(id);
            }
        }
    }
}