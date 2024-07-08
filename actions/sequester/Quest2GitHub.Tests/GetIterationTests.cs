using Quest2GitHub.Models;

namespace Quest2GitHub.Tests;
public class GetIterationTests
{
    private static QuestIteration[] _allIterations =
        [
        new QuestIteration { Id = Guid.Parse("d7470997-4db4-4146-95bd-1b02b8be20d2"), Name = "04 Apr", Path = """Content\CY_2022\04 Apr""" },
            new QuestIteration { Id = Guid.Parse("ade387a7-c9d1-4ef1-8130-dd4d3553efa9"), Name = " 05 May", Path = """Content\CY_2022\05 May""" },
            new QuestIteration { Id = Guid.Parse("8320be23-4cc6-45a1-b238-c5e076622870"), Name = " 06 Jun", Path = """Content\CY_2022\06 Jun""" },
            new QuestIteration { Id = Guid.Parse("57e35043-1d58-45cd-938f-6d4a124454d2"), Name = " 07 Jul", Path = """Content\CY_2022\07 Jul""" },
            new QuestIteration { Id = Guid.Parse("6c6263e9-8996-462b-8f81-a0f05630a282"), Name = " 08 Aug", Path = """Content\CY_2022\08 Aug""" },
            new QuestIteration { Id = Guid.Parse("e809d008-f2db-4c0e-9fb0-22eee3ae1831"), Name = " 09 Sep", Path = """Content\CY_2022\09 Sep""" },
            new QuestIteration { Id = Guid.Parse("ca46ea6b-3da8-4786-95b0-8367aaf1d942"), Name = " 10 Oct", Path = """Content\CY_2022\10 Oct""" },
            new QuestIteration { Id = Guid.Parse("d9da519b-ab94-4bb7-a280-c0c4e65fe60b"), Name = " 11 Nov", Path = """Content\CY_2022\11 Nov""" },
            new QuestIteration { Id = Guid.Parse("c622ef37-44ad-43e2-a128-9ab4ca6b6ee9"), Name = " 12 Dec", Path = """Content\CY_2022\12 Dec""" },
            new QuestIteration { Id = Guid.Parse("f6fdba55-fbe6-4250-8285-ebabc94145d3"), Name = " 01 Jan", Path = """Content\CY_2023\01 Jan""" },
            new QuestIteration { Id = Guid.Parse("d6bca9e6-1658-4fb1-8f41-9d7b5fcb27a6"), Name = " 02 Feb", Path = """Content\CY_2023\02 Feb""" },
            new QuestIteration { Id = Guid.Parse("3b2e41e9-6e1d-4492-b531-c584143e9ceb"), Name = " 03 Mar", Path = """Content\CY_2023\03 Mar""" },
            new QuestIteration { Id = Guid.Parse("03976f7e-3f34-4791-95f3-9081c7377716"), Name = " 04 Apr", Path = """Content\Gallium\FY23Q4\04 Apr""" },
            new QuestIteration { Id = Guid.Parse("0c948fc6-aa82-4c75-bede-e69cab231264"), Name = " 05 May", Path = """Content\Gallium\FY23Q4\05 May""" },
            new QuestIteration { Id = Guid.Parse("870ebccc-535e-48ff-beee-d90cfe1ecb34"), Name = " 06 Jun", Path = """Content\Gallium\FY23Q4\06 Jun""" },
            new QuestIteration { Id = Guid.Parse("4ff5a0b7-ef2c-49e6-9e9a-5e6e759e327e"), Name = " 07 July", Path = """Content\Gallium\FY24Q1\07 July""" },
            new QuestIteration { Id = Guid.Parse("8d6aa2e1-1236-485f-abb0-a8aaf3732751"), Name = " 08 Aug", Path = """Content\Gallium\FY24Q1\08 Aug""" },
            new QuestIteration { Id = Guid.Parse("a1068cfe-bb6d-47ec-aa67-4fb130d78193"), Name = " 09 Sep", Path = """Content\Gallium\FY24Q1\09 Sep""" },
            new QuestIteration { Id = Guid.Parse("ec694f64-abbd-45a5-b540-236fb3771338"), Name = " 10 Oct", Path = """Content\Germanium\FY24Q2\10 Oct""" },
            new QuestIteration { Id = Guid.Parse("8800d3bf-9478-4efa-b735-0c5fb685d515"), Name = " 11 Nov", Path = """Content\Germanium\FY24Q2\11 Nov""" },
            new QuestIteration { Id = Guid.Parse("b278fb17-8a93-4838-9224-cc1326501e89"), Name = " 12 Dec", Path = """Content\Germanium\FY24Q2\12 Dec""" },
            new QuestIteration { Id = Guid.Parse("bb02a733-b297-44e5-b326-b893700f0ea8"), Name = " 01 Jan", Path = """Content\Germanium\FY24Q3\01 Jan""" },
            new QuestIteration { Id = Guid.Parse("4ba3be79-97f3-4325-8845-b290ec447ba8"), Name = " 02 Feb", Path = """Content\Germanium\FY24Q3\02 Feb""" },
            new QuestIteration { Id = Guid.Parse("8add98b0-b760-4448-a315-6848563663fc"), Name = " 03 Mar", Path = """Content\Germanium\FY24Q3\03 Mar""" },
            new QuestIteration { Id = Guid.Parse("0ee548e5-7c70-44c0-afa9-d1a0be0441f2"), Name = " 04 Apr", Path = """Content\Dilithium\FY24Q4\04 Apr""" },
            new QuestIteration { Id = Guid.Parse("9c2ae4cd-47c3-41ca-a2e4-8a9301a9e291"), Name = " 05 May", Path = """Content\Dilithium\FY24Q4\05 May""" },
            new QuestIteration { Id = Guid.Parse("304e8a66-a53c-4997-b6fe-97071690434b"), Name = " 06 Jun", Path = """Content\Dilithium\FY24Q4\06 Jun""" },
            new QuestIteration { Id = Guid.Parse("410768bf-e1c3-46c1-bca2-73d917f010a2"), Name = " 07 Jul", Path = """Content\Dilithium\FY25Q1\07 Jul""" },
            new QuestIteration { Id = Guid.Parse("0b530b25-0970-4fd5-aeb2-891dff587264"), Name = " 08 Aug", Path = """Content\Dilithium\FY25Q1\08 Aug""" },
            new QuestIteration { Id = Guid.Parse("85d7fc73-4977-49b9-a84f-ea2211032b9e"), Name = " 09 Sep", Path = """Content\Dilithium\FY25Q1\09 Sep""" },
            new QuestIteration { Id = Guid.Parse("2cc96e64-21e5-4c76-a073-80df5945c199"), Name = " 10 Oct", Path = """Content\Selenium\FY25Q2\10 Oct""" },
            new QuestIteration { Id = Guid.Parse("b7591952-acee-400a-a51f-fc1215ca031d"), Name = " 11 Nov", Path = """Content\Selenium\FY25Q2\11 Nov""" },
            new QuestIteration { Id = Guid.Parse("b1e7d3b8-ab9a-4f98-a506-9a67ea3fd728"), Name = " 12 Dec", Path = """Content\Selenium\FY25Q2\12 Dec""" },
            new QuestIteration { Id = Guid.Parse("66727215-2522-4d69-b6e0-cb22bf22f4c2"), Name = " 01 Jan", Path = """Content\Selenium\FY25Q3\01 Jan""" },
            new QuestIteration { Id = Guid.Parse("0594f104-9d78-4b92-93db-f47610a9c515"), Name = " 02 Feb", Path = """Content\Selenium\FY25Q3\02 Feb""" },
            new QuestIteration { Id = Guid.Parse("08468117-ec32-4a80-af0b-763e84acf032"), Name = " 03 Mar", Path = """Content\Selenium\FY25Q3\03 Mar""" }
        ];


    [Fact]
    public static void ProjectIterationReturnsNullForInvalidMonth()
    {
        var actual = IssueExtensions.ProjectIteration("Foo", 2021, _allIterations);
        Assert.Null(actual);
    }

    [Fact]
    public static void ValidIterationReturnsCorrectIteration()
    {
        var actual = IssueExtensions.ProjectIteration("Feb", 2024, _allIterations);
        Assert.Equal("""Content\Germanium\FY24Q3\02 Feb""", actual?.Path);
    }
}


