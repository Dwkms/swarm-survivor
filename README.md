# Swarm Survivor

Unity 6로 제작한 2D Survivor 프로토타입이자, 성능 최적화 과정을 중심으로 정리한 포트폴리오입니다.

**문제 발견 → 가설 → 구현 → 측정 → 결과 분석 → 판단 → 기록**의 흐름으로, Object Pooling을 단순 적용하는 데 그치지 않고 생성·이동·대량 제거 상황을 분리해 frame time으로 비교했습니다.

## Quick Links

- [프로젝트 상세 소개 및 Gameplay](docs/README.md)
- [Windows Build v1.0.0](https://github.com/Dwkms/swarm-survivor/releases/tag/v1.0.0)
- [기능 및 구현 설명](docs/FEATURES.md)
- [설계 결정과 대안](docs/DECISIONS.md)
- [성능 측정 기록](docs/PERF_LOG.md)
- [문제 해결 기록](docs/TROUBLESHOOTING.md)
- [업데이트 로그](docs/UPDATELOG.md)

## Key Result

- Enemy 100 생성: OFF 1.24 / 1.93 ms, ON 0.93 / 2.90 ms
- 대량 제거·생성 worst: 약 25% 감소
- Enemy 900 이동 AVG: 뚜렷한 개선 없음
- Enemy 100 생성 GC Alloc: 0 KB

Pooling은 반복 생성 및 대량 제거·생성 비용 일부는 줄였지만, 활성 Enemy의 이동·Physics 비용은 제거하지 못했습니다.

Enemy 100 생성은 ON 3회차에 2.90 ms 이상치가 있어 단일 개선율로 결론내리지 않았습니다.

자세한 측정 조건과 원본 수치는 [docs/PERF_LOG.md](docs/PERF_LOG.md)를 참고하세요.
