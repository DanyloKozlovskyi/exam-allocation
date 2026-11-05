import { RoutePath, routes } from './routes';
import { apiRoutes } from './apiRoutes';

describe('routes', () => {
  it('builds app routes from RoutePath patterns', () => {
    expect(routes.rooms.edit('s1', 'c1', 'r1')).toBe('/sessions/s1/centers/c1/rooms/r1/edit');
    expect(routes.home()).toBe(RoutePath.Home);
  });

  it('builds API routes matching ApiRoutes.cs', () => {
    expect(apiRoutes.centers.create('s1')).toBe('sessions/s1/centers/create');
    expect(apiRoutes.allocations.roomSeating('a1', 'r1')).toBe('allocations/a1/rooms/r1');
    expect(apiRoutes.pins.pinAll('a1')).toBe('allocations/a1/pin-all');
  });
});
