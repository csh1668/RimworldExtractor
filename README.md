# RimworldExtractor: An assistance tool for extracting translation data for RimWorld.
### 림추출기: 림월드의 공식 컨텐츠나 모드의 번역 데이터를 추출하기 위한 번역 보조 툴입니다.

### [다운로드 (Downloads)](https://github.com/csh1668/RimworldExtractor/releases)

![스크린샷 2024-02-25 184956](https://github.com/csh1668/RimworldExtractor/assets/18442452/75e36e73-3cc7-425d-9755-36c399f6ee34)
기존에 존재하던 [알파 추출기](https://github.com/Han-ju/AlphaExtractor)의 여러 아쉬운 점을 보완하고자 개발하였습니다. \
\
예) Patches에 존재하는 번역 데이터를 제대로 처리하지 못하는 점, TranslationHandle이나 Full-List Translation을 지원하지 않는 점, XML 상속을 지원하지 않아 정확한 인덱싱이 불가한 점. TKey를 지원하지 않는 점 등

## 주요 기능 (Main Features)

- `Defs`, `Keyed`, `Strings`, `Patches`의 추출 가능
  1. ParentDef을 고려하기 위한 `XML 상속`, `참조 모드` 기능 지원
  2. 더 정확한 DefInjection을 위한 `TranslationHandle`, `Full-List Translation` 기능 지원
  3. 더 나은 추출을 위한 `TKey(SlateRef)` 지원
  4. XML Extension 모드의 `tKey`, `tToopTip` 지원
- 추출 시 `엑셀(.xlsx)`, `XML`, `XML(주석 포함)`의 저장 방식 지원
- XML과 엑셀 파일 간 상호 변환 가능 (XML -> 엑셀은 개발중)
