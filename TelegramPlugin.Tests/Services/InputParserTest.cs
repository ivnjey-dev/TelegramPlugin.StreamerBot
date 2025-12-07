using NUnit.Framework;
using TelegramPlugin.Services;
using TelegramPlugin.Enums;

namespace TelegramPlugin.Tests.Services;

[TestFixture]
public class InputParseTests
{
    private InputParser _parser;
    private string _tempImage;
    private string _tempVideo;


    [SetUp]
    public void Setup()
    {
        _parser = new InputParser();
        _tempImage = Path.GetTempFileName() + ".jpg";
        File.WriteAllText(_tempImage, "fake image data");

        _tempVideo = Path.GetTempFileName() + ".mp4";
        File.WriteAllText(_tempVideo, "fake video data");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_tempImage)) File.Delete(_tempImage);
        if (File.Exists(_tempVideo)) File.Delete(_tempVideo);
    }

    [Test]
    public void ResolveMedia_UrlWithoutExtension_ExplicitPhoto_ReturnsPhotoUrl()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "123" },
            { "tg_media_path", "https://site.com/dynamic/image" }, // Без расширения
            { "tg_media_type", "photo" } // Явно указан
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.MediaType, Is.EqualTo(MediaType.PhotoUrl));
    }

    [Test]
    public void ResolveMedia_UrlWithoutExtension_ExplicitVideo_ReturnsVideoUrl()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "123" },
            { "tg_media_path", "https://site.com/stream" },
            { "tg_media_type", "video" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.MediaType, Is.EqualTo(MediaType.VideoUrl));
    }

    [Test]
    public void ResolveMedia_UrlWithExtension_WithoutSchemes_ExpliciteType_Auto_ReturnsFailure()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "123" },
            { "tg_media_type", "photo" },
            { "tg_media_path", "site.com/unknown.jpg" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("local file does not exist"));
    }

    [Test]
    public void ResolveMedia_UrlWithExtension_WithoutSchemes_Auto_ReturnsFailure()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "123" },
            { "tg_media_path", "site.com/unknown.jpg" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.MediaType, Is.EqualTo(MediaType.Text));

        Assert.That(result.HasWarning, Is.True);
        Assert.That(result.WarningMessage, Does.Contain("not found (and not a valid URL)"));
    }

    [Test]
    public void ResolveMedia_UrlWithoutExtension_Auto_ReturnsFailure()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "123" },
            { "tg_media_path", "https://site.com/unknown" }
            // MediaType Auto (по умолчанию)
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Could not detect media type"));
    }

    [Test]
    public void ResolveMedia_UrlWithPhotoExtension_Auto_ReturnsPhotoUrl()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "123" },
            { "tg_media_path", "https://site.com/img.png" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.MediaType, Is.EqualTo(MediaType.PhotoUrl));
    }

    [Test]
    public void ResolveMedia_UrlWithQueryParams_Auto_ReturnsPhotoUrl()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "123" },
            { "tg_media_path", "https://site.com/img.jpg?token=abc&size=large" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.MediaType, Is.EqualTo(MediaType.PhotoUrl));
    }

    [Test]
    public void ResolveMedia_ExplicitPhoto_EmptyPath_ReturnsFailure()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "123" },
            { "tg_media_type", "photo" },
            { "tg_media_path", "" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("path is empty"));
    }

    [Test]
    public void ResolveMedia_Auto_EmptyPath_ReturnsText()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "123" },
            { "tg_media_type", "auto" },
            { "tg_media_path", "" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.MediaType, Is.EqualTo(MediaType.Text));
    }

    [Test]
    public void ResolveMedia_ExplicitPhoto_UrlWithTxtExtension_ReturnsFailure()
    {
        // Строгая проверка: просили фото, а ссылка заканчивается на .txt
        // В твоей новой логике:
        // if (isUrl && hasExtension && !looksLikePhoto) -> Failure

        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "123" },
            { "tg_media_type", "photo" },
            { "tg_media_path", "https://site.com/file.txt" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("File is not a photo"));
    }

    [Test]
    public void Parse_MissingChatId_ReturnsFailure()
    {
        var args = new Dictionary<string, object>();
        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("tg_chat_id"));
    }

    [Test]
    public void Parse_Missing_ChatId_ReturnsFailure()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_text", "Hello" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Contains.Substring("tg_chat_id is missing"));
    }

    [Test]
    public void Parse_InvalidChatId_ReturnsFailure()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "not-a-number" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void Parse_ValidChatId_Types()
    {
        var res1 = _parser.ParseSend(new Dictionary<string, object> { { "tg_chat_id", "-100123" } });
        Assert.That(res1.IsSuccess, Is.True);
        Assert.That(res1.Data.ChatId, Is.EqualTo(-100123));

        var res2 = _parser.ParseSend(new Dictionary<string, object> { { "tg_chat_id", -100123L } });
        Assert.That(res2.IsSuccess, Is.True);
        Assert.That(res2.Data.ChatId, Is.EqualTo(-100123));

        var res3 = _parser.ParseSend(new Dictionary<string, object> { { "tg_chat_id", -100123.0 } });
        Assert.That(res3.IsSuccess, Is.True);
    }


    [Test]
    public void Parse_CaseInsensitiveKeys_Works()
    {
        var args = new Dictionary<string, object>
        {
            { "TG_CHAT_ID", "12345" },
            { "Tg_TeXt", "Hello" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.ChatId, Is.EqualTo(12345));
        Assert.That(result.Data.Text, Is.EqualTo("Hello"));
    }


    [Test]
    public void Parse_Media_ExplicitPhoto_FileNotFound_ReturnsFailure()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "123" },
            { "tg_media_type", "photo" },
            { "tg_media_path", "non_existent.jpg" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Contains.Substring("file does not exist"));
    }

    [Test]
    public void Parse_Media_AutoDetect_Photo()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "123" },
            { "tg_media_path", _tempImage }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.MediaType, Is.EqualTo(MediaType.Photo));
    }

    [Test]
    public void Parse_Media_AutoDetect_Video()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "123" },
            { "tg_media_path", _tempVideo }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.MediaType, Is.EqualTo(MediaType.Video));
    }

    [Test]
    public void Parse_Media_WrongExtension_ReturnsFailure()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "123" },
            { "tg_media_type", "photo" },
            { "tg_media_path", _tempVideo }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Contains.Substring("File is not a photo"));
    }


    [Test]
    public void Parse_Buttons_Valid()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "123" },
            { "tg_btn_text0", "Google" }, { "tg_btn_url0", "https://google.com" },
            { "tg_btn_text1", "Yandex" }, { "tg_btn_url1", "https://ya.ru" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.Buttons, Is.Not.Null);
        Assert.That(result.Data.Buttons.Count, Is.EqualTo(1));
        Assert.That(result.Data.Buttons[0].Count, Is.EqualTo(2));
        Assert.That(result.Data.Buttons[0][0].Text, Is.EqualTo("Google"));
        Assert.That(result.Data.Buttons[0][1].Text, Is.EqualTo("Yandex"));
    }

    [Test]
    public void Parse_Buttons_MissingUrl_ReturnsFailure()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_ID", "123" },
            { "tg_btn_text0", "Google" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Contains.Substring("Missing URL"));
    }

    [Test]
    public void Parse_Buttons_Layout()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "123" },
            { "tg_btn_text0", "1" }, { "tg_btn_url0", "u" },
            { "tg_btn_text1", "2" }, { "tg_btn_url1", "u" },
            { "tg_btn_text2", "3" }, { "tg_btn_url2", "u" },
            { "tg_layout", "2, 1" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.True);
        var rows = result.Data.Buttons;

        Assert.That(rows.Count, Is.EqualTo(2));
        Assert.That(rows[0].Count, Is.EqualTo(2));
        Assert.That(rows[1].Count, Is.EqualTo(1));
    }

    // === BOOLEANS & NUMBERS ===

    [Test]
    public void Parse_Booleans_And_Ints()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_Id", "123" },
            { "tg_topic_ID", "42" },
            { "tg_delete_file", "True" }, // String bool
            { "tg_notification", true } // Native bool
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.TopicId, Is.EqualTo(42));
        Assert.That(result.Data.DeleteFile, Is.True);
        Assert.That(result.Data.Notification, Is.True);
    }

    [Test]
    public void Parse_ValidChatId_ReturnsSuccess()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", "-100123" },
            { "tg_text", "Test" }
        };
        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.ChatId, Is.EqualTo(-100123));
        Assert.That(result.Data.Text, Is.EqualTo("Test"));
    }

    [Test]
    public void Parse_Buttons_CollectsSequentialOnly()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", 123 },
            { "tg_btn_text0", "Zero" }, { "tg_btn_url0", "u0" },
            { "tg_btn_text1", "One" }, { "tg_btn_url1", "u1" },
            // 3 - игнорируется? так как 2 пропущен
            { "tg_btn_text3", "Three" }, { "tg_btn_url3", "u3" }
        };

        var result = _parser.ParseSend(args);
        var buttons = result.Data.Buttons;

        Assert.That(buttons.Count, Is.EqualTo(1));
        Assert.That(buttons[0].Count, Is.EqualTo(2));
        Assert.That(buttons[0][0].Text, Is.EqualTo("Zero"));
        Assert.That(buttons[0][1].Text, Is.EqualTo("One"));
    }

    [Test]
    public void Parse_ButtonMissingUrl_ReturnsFailure()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", 123 },
            { "tg_btn_text0", "BtnWithoutUrl" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Missing URL"));
    }

    [Test]
    public void Parse_Layout_SplitsCorrectly()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", 123 },
            { "tg_btn_text0", "A" }, { "tg_btn_url0", "u" },
            { "tg_btn_text1", "B" }, { "tg_btn_url1", "u" },
            { "tg_btn_text2", "C" }, { "tg_btn_url2", "u" },
            { "tg_layout", "2, 1" }
        };

        var result = _parser.ParseSend(args);
        var rows = result.Data.Buttons;

        Assert.That(rows.Count, Is.EqualTo(2), "Should have 2 rows");
        Assert.That(rows[0].Count, Is.EqualTo(2), "Row 1 should have 2 btns");
        Assert.That(rows[1].Count, Is.EqualTo(1), "Row 2 should have 1 btn");
        Assert.That(rows[0][0].Text, Is.EqualTo("A"));
        Assert.That(rows[1][0].Text, Is.EqualTo("C"));
    }

    [Test]
    public void ResolveMedia_ExplicitPhotoMissingFile_ReturnsFailure()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_Id", 123 },
            { "tg_media_type", "photo" },
            { "tg_media_path", "non_existent.jpg" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("file does not exist"));
    }

    [Test]
    public void ResolveMedia_AutoMissingFile_ReturnsTextMode()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", 123 },
            { "tg_media_type", "auto" },
            { "tg_media_path", "non_existent.jpg" }
        };

        var result = _parser.ParseSend(args);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.MediaType, Is.EqualTo(MediaType.Text));
    }
}