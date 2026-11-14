import { useLocation } from 'react-router-dom';

export const LOCATION_TEST_ID = 'test-location';

/** Test-only: prints the current router location so navigation can be asserted. */
const LocationDisplay = () => {
  const location = useLocation();
  return <div data-qa={LOCATION_TEST_ID}>{`${location.pathname}${location.search}`}</div>;
};

export default LocationDisplay;
