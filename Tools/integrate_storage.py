from pathlib import Path
root=Path(__file__).resolve().parents[1]
for tab,index in [('Game1','Tab1'),('Game2','Tab2')]:
    p=root/'Assets/Scripts/Assembly-CSharp'/tab/'Rms.cs'
    s=p.read_text(encoding='utf-8')
    s=s.replace('string value = ByteArrayToString(ArrayCast.cast(data));',f'''#if UNITY_WEBGL && !UNITY_EDITOR
            NroWeb.BrowserSaveStore.Write("{index}", filename, ArrayCast.cast(data));
            return;
#endif
            string value = ByteArrayToString(ArrayCast.cast(data));''')
    s=s.replace('string @string = PlayerPrefs.GetString(filename + TabType.'+index+');',f'''#if UNITY_WEBGL && !UNITY_EDITOR
            var saved = NroWeb.BrowserSaveStore.Read("{index}",filename);
            return saved == null ? null : ArrayCast.cast(saved);
#else
            string @string = PlayerPrefs.GetString(filename + TabType.{index});''')
    s=s.replace('return ArrayCast.cast(array);\n        }','return ArrayCast.cast(array);\n#endif\n        }',1)
    s=s.replace('PlayerPrefs.DeleteAll();',f'''#if UNITY_WEBGL && !UNITY_EDITOR
            NroWeb.BrowserSaveStore.Clear("{index}");
#else
            PlayerPrefs.DeleteAll();
#endif''')
    for key in ['path','name']:
        s=s.replace('PlayerPrefs.DeleteKey('+key+');',f'''#if UNITY_WEBGL && !UNITY_EDITOR
            NroWeb.BrowserSaveStore.Delete("{index}",{key});
#else
            PlayerPrefs.DeleteKey({key});
#endif''')
    p.write_text(s,encoding='utf-8')
    p=root/'Assets/Scripts/Assembly-CSharp'/tab/'GameCanvas.cs';s=p.read_text(encoding='utf-8')
    # Keep PRJ's isTouch=true: this enables mouse hit tests as well as touch.
    p.write_text(s,encoding='utf-8')
print('Per-tab IndexedDB save branches integrated')
